// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;

using ILCompiler.Logging;

using Internal.IL;
using Internal.TypeSystem;

using BindingFlags = System.Reflection.BindingFlags;
using NodeFactory = ILCompiler.DependencyAnalysis.NodeFactory;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using CustomAttributeValue = System.Reflection.Metadata.CustomAttributeValue<Internal.TypeSystem.TypeDesc>;
using CustomAttributeTypedArgument = System.Reflection.Metadata.CustomAttributeTypedArgument<Internal.TypeSystem.TypeDesc>;
using CustomAttributeNamedArgumentKind = System.Reflection.Metadata.CustomAttributeNamedArgumentKind;
using InteropTypes = Internal.TypeSystem.Interop.InteropTypes;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using WellKnownType = ILLink.Shared.TypeSystemProxy.WellKnownType;
using System.Collections.Immutable;

namespace ILCompiler.Dataflow
{
    class ReflectionMethodBodyScanner : MethodBodyScanner
    {
        private readonly FlowAnnotations _annotations;
        private readonly Logger _logger;
        private readonly NodeFactory _factory;
        private readonly ReflectionMarker _reflectionMarker;
        private const string RequiresUnreferencedCodeAttribute = nameof(RequiresUnreferencedCodeAttribute);
        private const string RequiresDynamicCodeAttribute = nameof(RequiresDynamicCodeAttribute);
        private const string RequiresAssemblyFilesAttribute = nameof(RequiresAssemblyFilesAttribute);

        public static bool RequiresReflectionMethodBodyScannerForCallSite(FlowAnnotations flowAnnotations, MethodDesc methodDefinition)
        {
            return Intrinsics.GetIntrinsicIdForMethod(methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
                flowAnnotations.RequiresDataflowAnalysis(methodDefinition) ||
                methodDefinition.DoesMethodRequire(RequiresUnreferencedCodeAttribute, out _) ||
                methodDefinition.DoesMethodRequire(RequiresDynamicCodeAttribute, out _) ||
                methodDefinition.IsPInvoke;
        }

        public static bool RequiresReflectionMethodBodyScannerForMethodBody(FlowAnnotations flowAnnotations, MethodDesc methodDefinition)
        {
            return Intrinsics.GetIntrinsicIdForMethod(methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
                flowAnnotations.RequiresDataflowAnalysis(methodDefinition);
        }

        public static bool RequiresReflectionMethodBodyScannerForAccess(FlowAnnotations flowAnnotations, FieldDesc fieldDefinition)
        {
            return flowAnnotations.RequiresDataflowAnalysis(fieldDefinition) ||
                fieldDefinition.DoesFieldRequire(RequiresUnreferencedCodeAttribute, out _) ||
                fieldDefinition.DoesFieldRequire(RequiresDynamicCodeAttribute, out _);
        }

        void CheckAndReportRequires(TypeSystemEntity calledMember, in MessageOrigin origin, string requiresAttributeName)
        {
            // If the caller of a method is already marked with `Requires` a new warning should not
            // be produced for the callee.
            if (ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, requiresAttributeName))
                return;

            if (!calledMember.DoesMemberRequire(requiresAttributeName, out var requiresAttribute))
                return;

            DiagnosticId diagnosticId = requiresAttributeName switch
            {
                RequiresUnreferencedCodeAttribute => DiagnosticId.RequiresUnreferencedCode,
                RequiresDynamicCodeAttribute => DiagnosticId.RequiresDynamicCode,
                RequiresAssemblyFilesAttribute => DiagnosticId.RequiresAssemblyFiles,
                _ => throw new NotImplementedException($"{requiresAttributeName} is not a valid supported Requires attribute"),
            };

            ReportRequires(calledMember.GetDisplayName(), origin, diagnosticId, requiresAttribute);
        }

        internal static bool ShouldSuppressAnalysisWarningsForRequires(TypeSystemEntity originMember, string requiresAttribute)
        {
            // Check if the current scope method has Requires on it
            // since that attribute automatically suppresses all trim analysis warnings.
            // Check both the immediate origin method as well as suppression context method
            // since that will be different for compiler generated code.
            if (originMember == null)
                return false;

            if (originMember is not MethodDesc method)
                return false;

            if (method.IsInRequiresScope(requiresAttribute))
                return true;

            MethodDesc userMethod = ILCompiler.Logging.CompilerGeneratedState.GetUserDefinedMethodForCompilerGeneratedMember(method);
            if (userMethod != null &&
                userMethod.IsInRequiresScope(requiresAttribute))
                return true;

            return false;
        }

        void ReportRequires(string displayName, in MessageOrigin currentOrigin, DiagnosticId diagnosticId, CustomAttributeValue<TypeDesc>? requiresAttribute)
        {
            string arg1 = MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage((CustomAttributeValue<TypeDesc>)requiresAttribute));
            string arg2 = MessageFormat.FormatRequiresAttributeUrlArg(DiagnosticUtilities.GetRequiresAttributeUrl((CustomAttributeValue<TypeDesc>)requiresAttribute));

            _logger.LogWarning(currentOrigin, diagnosticId, displayName, arg1, arg2);
        }

        private enum ScanningPurpose
        {
            Default,
            GetTypeDataflow,
        }

        private ScanningPurpose _purpose;

        private ReflectionMethodBodyScanner(NodeFactory factory, FlowAnnotations annotations, Logger logger, ScanningPurpose purpose = ScanningPurpose.Default)
        {
            _annotations = annotations;
            _logger = logger;
            _factory = factory;
            _purpose = purpose;
            _reflectionMarker = new ReflectionMarker(logger, factory, annotations, purpose == ScanningPurpose.GetTypeDataflow);
        }

        public static DependencyList ScanAndProcessReturnValue(NodeFactory factory, FlowAnnotations annotations, Logger logger, MethodIL methodBody)
        {
            var scanner = new ReflectionMethodBodyScanner(factory, annotations, logger);

            Debug.Assert(methodBody.GetMethodILDefinition() == methodBody);
            if (methodBody.OwningMethod.HasInstantiation || methodBody.OwningMethod.OwningType.HasInstantiation)
            {
                // We instantiate the body over the generic parameters.
                //
                // This will transform references like "call Foo<!0>.Method(!0 arg)" into
                // "call Foo<T>.Method(T arg)". We do this to avoid getting confused about what
                // context the generic variables refer to - in the above example, we would see
                // two !0's - one refers to the generic parameter of the type that owns the method with
                // the call, but the other one (in the signature of "Method") actually refers to
                // the generic parameter of Foo.
                //
                // If we don't do this translation, retrieving the signature of the called method
                // would attempt to do bogus substitutions.
                //
                // By doing the following transformation, we ensure we don't see the generic variables
                // that need to be bound to the context of the currently analyzed method.
                methodBody = new InstantiatedMethodIL(methodBody.OwningMethod, methodBody);
            }

            scanner.Scan(methodBody);

            if (!methodBody.OwningMethod.Signature.ReturnType.IsVoid)
            {
                var method = methodBody.OwningMethod;
                var methodReturnValue = scanner._annotations.GetMethodReturnValue(method);
                if (methodReturnValue.DynamicallyAccessedMemberTypes != 0)
                {
                    var diagnosticContext = new DiagnosticContext(new MessageOrigin(method), !ShouldSuppressAnalysisWarningsForRequires(method, RequiresUnreferencedCodeAttribute), scanner._logger);
                    scanner.RequireDynamicallyAccessedMembers(diagnosticContext, scanner.ReturnValue, methodReturnValue, new MethodReturnOrigin(method));
                }
            }

            return scanner._reflectionMarker.Dependencies;
        }

        public static DependencyList ProcessAttributeDataflow(NodeFactory factory, FlowAnnotations annotations, Logger logger, MethodDesc method, CustomAttributeValue arguments)
        {
            DependencyList result = null;

            // First do the dataflow for the constructor parameters if necessary.
            if (annotations.RequiresDataflowAnalysis(method))
            {
                for (int i = 0; i < method.Signature.Length; i++)
                {
                    var parameterValue = annotations.GetMethodParameterValue(method, i);
                    if (parameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                    {
                        MultiValue value = GetValueNodeForCustomAttributeArgument(arguments.FixedArguments[i].Value);
                        var diagnosticContext = new DiagnosticContext(new MessageOrigin(method), diagnosticsEnabled: true, logger);
                        var scanner = new ReflectionMethodBodyScanner(factory, annotations, logger);
                        scanner.RequireDynamicallyAccessedMembers(diagnosticContext, value, parameterValue, parameterValue.ParameterOrigin);
                    }
                }
            }

            // Named arguments next
            TypeDesc attributeType = method.OwningType;
            foreach (var namedArgument in arguments.NamedArguments)
            {
                DynamicallyAccessedMemberTypes annotation = DynamicallyAccessedMemberTypes.None;
                MultiValue targetValues = new();
                Origin targetContext = null;
                if (namedArgument.Kind == CustomAttributeNamedArgumentKind.Field)
                {
                    FieldDesc field = attributeType.GetField(namedArgument.Name);
                    if (field != null)
                    {
                        targetValues = GetFieldValue(field, annotations);
                        targetContext = new FieldOrigin(field);
                    }
                }
                else
                {
                    Debug.Assert(namedArgument.Kind == CustomAttributeNamedArgumentKind.Property);
                    PropertyPseudoDesc property = ((MetadataType)attributeType).GetProperty(namedArgument.Name, null);
                    MethodDesc setter = property.SetMethod;
                    if (setter != null && setter.Signature.Length > 0 && !setter.Signature.IsStatic)
                    {
                        targetValues = annotations.GetMethodParameterValue(setter, 0);
                        targetContext = new ParameterOrigin(setter, 1);
                    }
                }

                if (annotation != DynamicallyAccessedMemberTypes.None)
                {
                    MultiValue valueNode = GetValueNodeForCustomAttributeArgument(namedArgument.Value);
                    foreach (var targetValueCandidate in targetValues)
                    {
                        if (targetValueCandidate is not ValueWithDynamicallyAccessedMembers targetValue)
                            continue;

                        var diagnosticContext = new DiagnosticContext(new MessageOrigin(method), diagnosticsEnabled: true, logger);
                        var scanner = new ReflectionMethodBodyScanner(factory, annotations, logger);
                        scanner.RequireDynamicallyAccessedMembers(diagnosticContext, valueNode, targetValue, targetContext);
                        if (result == null)
                        {
                            result = scanner._reflectionMarker.Dependencies;
                        }
                        else
                        {
                            result.AddRange(scanner._reflectionMarker.Dependencies);
                        }
                    }
                }
            }

            return result;
        }

        public static DependencyList ProcessTypeGetTypeDataflow(NodeFactory factory, FlowAnnotations flowAnnotations, Logger logger, MetadataType type)
        {
            DynamicallyAccessedMemberTypes annotation = flowAnnotations.GetTypeAnnotation(type);
            Debug.Assert(annotation != DynamicallyAccessedMemberTypes.None);
            var reflectionMarker = new ReflectionMarker(logger, factory, flowAnnotations, true);
            reflectionMarker.MarkTypeForDynamicallyAccessedMembers(new MessageOrigin(type), type, annotation, new TypeOrigin(type));
            return reflectionMarker.Dependencies;
        }

        static MultiValue GetValueNodeForCustomAttributeArgument(object argument)
        {
            SingleValue result = null;
            if (argument is TypeDesc td)
            {
                result = new SystemTypeValue(td);
            }
            else if (argument is string str)
            {
                result = new KnownStringValue(str);
            }
            else
            {
                Debug.Assert(argument is null);
                result = NullValue.Instance;
            }

            Debug.Assert(result != null);
            return result;
        }

        public static DependencyList ProcessGenericArgumentDataFlow(NodeFactory factory, FlowAnnotations flowAnnotations, Logger logger, GenericParameterDesc genericParameter, TypeDesc genericArgument, TypeSystemEntity source)
        {
            var scanner = new ReflectionMethodBodyScanner(factory, flowAnnotations, logger);

            var genericParameterValue = flowAnnotations.GetGenericParameterValue(genericParameter);
            Debug.Assert(genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None);

            MultiValue genericArgumentValue = scanner.GetTypeValueNodeFromGenericArgument(genericArgument);

            // TODO: This should use ShouldEnableReflectionPatternReporting to conditionally disable the diagnostics - but we don't have the right
            // context to do so yet.
            var diagnosticContext = new DiagnosticContext(new MessageOrigin(source), diagnosticsEnabled: true, logger);
            var origin = new GenericParameterOrigin(genericParameter);
            scanner.RequireDynamicallyAccessedMembers(diagnosticContext, genericArgumentValue, genericParameterValue, origin);

            return scanner._reflectionMarker.Dependencies;
        }

        MultiValue GetTypeValueNodeFromGenericArgument(TypeDesc genericArgument)
        {
            if (genericArgument is GenericParameterDesc inputGenericParameter)
            {
                return _annotations.GetGenericParameterValue(inputGenericParameter);
            }
            else if (genericArgument is MetadataType genericArgumentType)
            {
                if (genericArgumentType.IsTypeOf(WellKnownType.System_Nullable_T))
                {
                    var innerGenericArgument = genericArgumentType.Instantiation.Length == 1 ? genericArgumentType.Instantiation[0] : null;
                    switch (innerGenericArgument)
                    {
                        case GenericParameterDesc gp:
                            return new NullableValueWithDynamicallyAccessedMembers(genericArgumentType,
                                new GenericParameterValue(gp, _annotations.GetGenericParameterAnnotation(gp)));

                        case TypeDesc underlyingType:
                            return new NullableSystemTypeValue(genericArgumentType, new SystemTypeValue(underlyingType));
                    }
                }
                // All values except for Nullable<T>, including Nullable<> (with no type arguments)
                return new SystemTypeValue(genericArgumentType);
            }
            else
            {
                return UnknownValue.Instance;
            }
        }

        protected override void WarnAboutInvalidILInMethod(MethodIL method, int ilOffset)
        {
            // Serves as a debug helper to make sure valid IL is not considered invalid.
            //
            // The .NET Native compiler used to warn if it detected invalid IL during treeshaking,
            // but the warnings were often triggered in autogenerated dead code of a major game engine
            // and resulted in support calls. No point in warning. If the code gets exercised at runtime,
            // an InvalidProgramException will likely be raised.
            Debug.Fail("Invalid IL or a bug in the scanner");
        }

        protected override ValueWithDynamicallyAccessedMembers GetMethodParameterValue(MethodDesc method, int parameterIndex)
            => GetMethodParameterValue(method, parameterIndex, _annotations.GetParameterAnnotation(method, parameterIndex));

        // TODO: This also works for "this" parameters in the linker!
        ValueWithDynamicallyAccessedMembers GetMethodParameterValue(MethodDesc method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
        {
            return _annotations.GetMethodParameterValue(method, parameterIndex, dynamicallyAccessedMemberTypes);
        }

        static MultiValue GetFieldValue(FieldDesc field, FlowAnnotations annotations)
        {
            switch (field.Name)
            {
                case "EmptyTypes" when field.OwningType.IsTypeOf(ILLink.Shared.TypeSystemProxy.WellKnownType.System_Type):
                    {
                        return ArrayValue.Create(0, field.OwningType);
                    }
                case "Empty" when field.OwningType.IsTypeOf(ILLink.Shared.TypeSystemProxy.WellKnownType.System_String):
                    {
                        return new KnownStringValue(string.Empty);
                    }

                default:
                    {
                        DynamicallyAccessedMemberTypes memberTypes = annotations.GetFieldAnnotation(field);
                        return new FieldValue(field, memberTypes);
                    }
            }
        }

        protected override MultiValue GetFieldValue(FieldDesc field)
        {
            return GetFieldValue(field, _annotations);
        }

        protected override void HandleStoreField(MethodIL methodBody, int offset, FieldValue field, MultiValue valueToStore)
        {
            if (field.DynamicallyAccessedMemberTypes != 0)
            {
                var diagnosticContext = new DiagnosticContext(new MessageOrigin(methodBody, offset), !ShouldSuppressAnalysisWarningsForRequires(methodBody.OwningMethod, RequiresUnreferencedCodeAttribute), _logger);
                RequireDynamicallyAccessedMembers(diagnosticContext, valueToStore, field, new FieldOrigin(field.Field));
            }
            CheckAndReportRequires(field, new MessageOrigin(methodBody.OwningMethod), RequiresUnreferencedCodeAttribute);
            CheckAndReportRequires(field, new MessageOrigin(methodBody.OwningMethod), RequiresDynamicCodeAttribute);
        }

        protected override void HandleStoreParameter(MethodIL method, int offset, MethodParameterValue parameter, MultiValue valueToStore)
        {
            if (parameter.DynamicallyAccessedMemberTypes != 0)
            {
                var diagnosticContext = new DiagnosticContext(new MessageOrigin(method, offset), !ShouldSuppressAnalysisWarningsForRequires(method.OwningMethod, RequiresUnreferencedCodeAttribute), _logger);
                RequireDynamicallyAccessedMembers(diagnosticContext, valueToStore, parameter, parameter.ParameterOrigin);
            }
        }

        public override bool HandleCall(MethodIL callingMethodBody, MethodDesc calledMethod, ILOpcode operation, int offset, ValueNodeList methodParams, out MultiValue methodReturnValue)
        {
            methodReturnValue = null;
            MultiValue? maybeMethodReturnValue = null;

            var callingMethodDefinition = callingMethodBody.OwningMethod;
            bool shouldEnableReflectionWarnings = !ShouldSuppressAnalysisWarningsForRequires(callingMethodDefinition, RequiresUnreferencedCodeAttribute);
            var reflectionContext = new ReflectionPatternContext(_logger, shouldEnableReflectionWarnings, callingMethodBody, offset, new MethodOrigin(calledMethod));

            DynamicallyAccessedMemberTypes returnValueDynamicallyAccessedMemberTypes = 0;

            try
            {
                bool requiresDataFlowAnalysis = _annotations.RequiresDataflowAnalysis(calledMethod);
                returnValueDynamicallyAccessedMemberTypes = requiresDataFlowAnalysis ?
                    _annotations.GetReturnParameterAnnotation(calledMethod) : 0;

                var diagnosticContext = new DiagnosticContext(new MessageOrigin(callingMethodBody, offset), shouldEnableReflectionWarnings, _logger);
                var handleCallAction = new HandleCallAction(_annotations, _reflectionMarker, diagnosticContext, callingMethodDefinition, new MethodOrigin(calledMethod));

                var intrinsicId = Intrinsics.GetIntrinsicIdForMethod(calledMethod);
                switch (intrinsicId)
                {
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
                        && calledMethod.OwningType.IsTypeOf(WellKnownType.System_Type)
                        && calledMethod.Signature[0].IsTypeOf("System.Reflection.BindingFlags")
                        && !calledMethod.Signature.IsStatic:
                    case var fieldPropertyOrEvent when (fieldPropertyOrEvent == IntrinsicId.Type_GetField || fieldPropertyOrEvent == IntrinsicId.Type_GetProperty || fieldPropertyOrEvent == IntrinsicId.Type_GetEvent)
                        && calledMethod.OwningType.IsTypeOf(WellKnownType.System_Type)
                        && calledMethod.Signature[0].IsTypeOf(WellKnownType.System_String)
                        && !calledMethod.Signature.IsStatic:
                    case var getRuntimeMember when getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent
                        || getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField
                        || getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod
                        || getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty:
                    case IntrinsicId.Type_GetMember:
                    case IntrinsicId.Type_GetMethod:
                    case IntrinsicId.Type_GetNestedType:
                    case IntrinsicId.Nullable_GetUnderlyingType:
                    case IntrinsicId.Expression_Property when calledMethod.HasParameterOfType(1, "System.Reflection.MethodInfo"):
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
                    case IntrinsicId.Assembly_CreateInstance:
                        {
                            var instanceValue = MultiValueLattice.Top;
                            IReadOnlyList<MultiValue> parameterValues = methodParams;
                            if (!calledMethod.Signature.IsStatic)
                            {
                                instanceValue = methodParams[0];
                                parameterValues = parameterValues.Skip(1).ToImmutableList();
                            }
                            return handleCallAction.Invoke(calledMethod, instanceValue, parameterValues, out methodReturnValue, out _);
                        }

                    case IntrinsicId.None:
                        {
                            if (calledMethod.IsPInvoke)
                            {
                                // Is the PInvoke dangerous?
                                ParameterMetadata[] paramMetadata = calledMethod.GetParameterMetadata();

                                ParameterMetadata returnParamMetadata = Array.Find(paramMetadata, m => m.Index == 0);

                                bool comDangerousMethod = IsComInterop(returnParamMetadata.MarshalAsDescriptor, calledMethod.Signature.ReturnType);
                                for (int paramIndex = 0; paramIndex < calledMethod.Signature.Length; paramIndex++)
                                {
                                    MarshalAsDescriptor marshalAsDescriptor = null;
                                    for (int metadataIndex = 0; metadataIndex < paramMetadata.Length; metadataIndex++)
                                    {
                                        if (paramMetadata[metadataIndex].Index == paramIndex + 1)
                                            marshalAsDescriptor = paramMetadata[metadataIndex].MarshalAsDescriptor;
                                    }

                                    comDangerousMethod |= IsComInterop(marshalAsDescriptor, calledMethod.Signature[paramIndex]);
                                }

                                if (comDangerousMethod)
                                {
                                    diagnosticContext.AddDiagnostic(DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethod.GetDisplayName());
                                }
                            }

                            CheckAndReportRequiresAttributes(shouldEnableReflectionWarnings, shouldEnableAotWarnings, callingMethodBody, offset, calledMethod);

                            var instanceValue = MultiValueLattice.Top;
                            IReadOnlyList<MultiValue> parameterValues = methodParams;
                            if (!calledMethod.Signature.IsStatic)
                            {
                                instanceValue = methodParams[0];
                                parameterValues = parameterValues.Skip(1).ToImmutableList();
                            }
                            return handleCallAction.Invoke(calledMethod, instanceValue, parameterValues, out methodReturnValue, out _);
                        }

                    case IntrinsicId.TypeDelegator_Ctor:
                        {
                            // This is an identity function for analysis purposes
                            if (operation == ILOpcode.newobj)
                                methodReturnValue = methodParams[1];
                        }
                        break;

                    case IntrinsicId.Array_Empty:
                        {
                            methodReturnValue = new ArrayValue(new ConstIntValue(0), calledMethod.Instantiation[0]);
                        }
                        break;


                    //
                    // System.Type
                    //
                    // Type MakeGenericType (params Type[] typeArguments)
                    //
                    case IntrinsicId.Type_MakeGenericType:
                        {
                            reflectionContext.AnalyzingPattern();
                            foreach (var value in methodParams[0].UniqueValues())
                            {
                                if (value is SystemTypeValue typeValue)
                                {
                                    if (AnalyzeGenericInstantiationTypeArray(methodParams[1], ref reflectionContext, calledMethod, typeValue.TypeRepresented.GetTypeDefinition().Instantiation))
                                    {
                                        reflectionContext.RecordHandledPattern();
                                    }
                                    else
                                    {
                                        bool hasUncheckedAnnotation = false;
                                        foreach (GenericParameterDesc genericParameter in typeValue.TypeRepresented.GetTypeDefinition().Instantiation)
                                        {
                                            if (_annotations.GetGenericParameterAnnotation(genericParameter) != DynamicallyAccessedMemberTypes.None ||
                                                (genericParameter.HasDefaultConstructorConstraint && !typeValue.TypeRepresented.IsNullable))
                                            {
                                                // If we failed to analyze the array, we go through the analyses again
                                                // and intentionally ignore one particular annotation:
                                                // Special case: Nullable<T> where T : struct
                                                //  The struct constraint in C# implies new() constraints, but Nullable doesn't make a use of that part.
                                                //  There are several places even in the framework where typeof(Nullable<>).MakeGenericType would warn
                                                //  without any good reason to do so.
                                                hasUncheckedAnnotation = true;
                                                break;
                                            }
                                        }
                                        if (hasUncheckedAnnotation)
                                        {
                                            reflectionContext.RecordUnrecognizedPattern(
                                                    (int)DiagnosticId.MakeGenericType,
                                                    new DiagnosticString(DiagnosticId.MakeGenericType).GetMessage(calledMethod.GetDisplayName()));
                                        }
                                    }

                                    // We haven't found any generic parameters with annotations, so there's nothing to validate.
                                    reflectionContext.RecordHandledPattern();
                                }
                                else if (value == NullValue.Instance)
                                    reflectionContext.RecordHandledPattern();
                                else
                                {
                                    // We have no way to "include more" to fix this if we don't know, so we have to warn
                                    reflectionContext.RecordUnrecognizedPattern(
                                        (int)DiagnosticId.MakeGenericType,
                                        new DiagnosticString(DiagnosticId.MakeGenericType).GetMessage(calledMethod.GetDisplayName()));
                                }
                            }

                            CheckAndReportRequires(calledMethod, new MessageOrigin(callingMethodBody, offset), RequiresDynamicCodeAttribute);

                            // We don't want to lose track of the type
                            // in case this is e.g. Activator.CreateInstance(typeof(Foo<>).MakeGenericType(...));
                            methodReturnValue = methodParams[0];
                        }
                        break;

                    //
                    // System.Enum
                    //
                    // static GetValues (Type)
                    //
                    case IntrinsicId.Enum_GetValues:
                        {
                            // Enum.GetValues returns System.Array, but it's the array of the enum type under the hood
                            // and people depend on this undocumented detail (could have returned enum of the underlying
                            // type instead).
                            //
                            // At least until we have shared enum code, this needs extra handling to get it right.
                            foreach (var value in methodParams[0].UniqueValues())
                            {
                                if (value is SystemTypeValue systemTypeValue
                                    && !systemTypeValue.TypeRepresented.IsGenericDefinition
                                    && !systemTypeValue.TypeRepresented.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                                {
                                    if (systemTypeValue.TypeRepresented.IsEnum)
                                    {
                                        _reflectionMarker.Dependencies.Add(_factory.ConstructedTypeSymbol(systemTypeValue.TypeRepresented.MakeArrayType()), "Enum.GetValues");
                                    }
                                }
                                else
                                    CheckAndReportRequires(calledMethod, new MessageOrigin(callingMethodBody, offset),RequiresDynamicCodeAttribute);
                            }
                        }
                        break;

                    //
                    // System.Runtime.InteropServices.Marshal
                    //
                    // static SizeOf (Type)
                    // static PtrToStructure (IntPtr, Type)
                    // static DestroyStructure (IntPtr, Type)
                    // static OffsetOf (Type, string)
                    //
                    case IntrinsicId.Marshal_SizeOf:
                    case IntrinsicId.Marshal_PtrToStructure:
                    case IntrinsicId.Marshal_DestroyStructure:
                    case IntrinsicId.Marshal_OffsetOf:
                        {
                            int paramIndex = intrinsicId == IntrinsicId.Marshal_SizeOf
                                || intrinsicId == IntrinsicId.Marshal_OffsetOf
                                ? 0 : 1;

                            // We need the data to do struct marshalling.
                            foreach (var value in methodParams[paramIndex].UniqueValues())
                            {
                                if (value is SystemTypeValue systemTypeValue
                                    && !systemTypeValue.TypeRepresented.IsGenericDefinition
                                    && !systemTypeValue.TypeRepresented.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                                {
                                    if (systemTypeValue.TypeRepresented.IsDefType)
                                    {
                                        _reflectionMarker.Dependencies.Add(_factory.StructMarshallingData((DefType)systemTypeValue.TypeRepresented), "Marshal API");
                                    }
                                }
                                else
                                    CheckAndReportRequires(calledMethod, new MessageOrigin(callingMethodBody, offset), RequiresDynamicCodeAttribute);
                            }
                        }
                        break;

                    //
                    // System.Runtime.InteropServices.Marshal
                    //
                    // static GetDelegateForFunctionPointer (IntPtr, Type)
                    //
                    case IntrinsicId.Marshal_GetDelegateForFunctionPointer:
                        {
                            // We need the data to do delegate marshalling.
                            foreach (var value in methodParams[1].UniqueValues())
                            {
                                if (value is SystemTypeValue systemTypeValue
                                    && !systemTypeValue.TypeRepresented.IsGenericDefinition
                                    && !systemTypeValue.TypeRepresented.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                                {
                                    if (systemTypeValue.TypeRepresented.IsDefType)
                                    {
                                        _reflectionMarker.Dependencies.Add(_factory.DelegateMarshallingData((DefType)systemTypeValue.TypeRepresented), "Marshal API");
                                    }
                                }
                                else
                                    CheckAndReportRequires(calledMethod, new MessageOrigin(callingMethodBody, offset), RequiresDynamicCodeAttribute);
                            }
                        }
                        break;

                    //
                    // System.Object
                    // 
                    // GetType()
                    //
                    case IntrinsicId.Object_GetType:
                        {
                            foreach (var valueNode in methodParams[0].UniqueValues())
                            {
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

                                TypeDesc staticType = valueNode.StaticType;
                                if (staticType is null || (!staticType.IsDefType && !staticType.IsArray))
                                {
                                    // We don't know anything about the type GetType was called on. Track this as a usual "result of a method call without any annotations"
                                    methodReturnValue = MergePointValue.MergeValues(methodReturnValue, new MethodReturnValue(calledMethod, DynamicallyAccessedMemberTypes.None));
                                }
                                else if (staticType.IsSealed() || staticType.IsTypeOf("System", "Delegate"))
                                {
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
                                    methodReturnValue = MergePointValue.MergeValues(methodReturnValue, new SystemTypeValue(staticType));
                                }
                                else
                                {
                                    reflectionContext.AnalyzingPattern();

                                    Debug.Assert(staticType is MetadataType || staticType.IsArray);
                                    MetadataType closestMetadataType = staticType is MetadataType mdType ?
                                        mdType : (MetadataType)_factory.TypeSystemContext.GetWellKnownType(WellKnownType.Array);

                                    var annotation = _annotations.GetTypeAnnotation(staticType);

                                    if (annotation != default)
                                    {
                                        _reflectionMarker.Dependencies.Add(_factory.ObjectGetTypeFlowDependencies(closestMetadataType), "GetType called on this type");
                                    }

                                    reflectionContext.RecordHandledPattern();

                                    // Return a value which is "unknown type" with annotation. For now we'll use the return value node
                                    // for the method, which means we're loosing the information about which staticType this
                                    // started with. For now we don't need it, but we can add it later on.
                                    methodReturnValue = MergePointValue.MergeValues(methodReturnValue, new MethodReturnValue(calledMethod, annotation));
                                }
                            }
                        }
                        break;

                    //
                    // System.Reflection.MethodInfo
                    //
                    // MakeGenericMethod (Type[] typeArguments)
                    //
                    case IntrinsicId.MethodInfo_MakeGenericMethod:
                        {
                            reflectionContext.AnalyzingPattern();

                            foreach (var methodValue in methodParams[0].UniqueValues())
                            {
                                if (methodValue is SystemReflectionMethodBaseValue methodBaseValue)
                                {
                                    ValidateGenericMethodInstantiation(ref reflectionContext, methodBaseValue.MethodRepresented, methodParams[1], calledMethod);
                                }
                                else if (methodValue == NullValue.Instance)
                                {
                                    reflectionContext.RecordHandledPattern();
                                }
                                else
                                {
                                    // We don't know what method the `MakeGenericMethod` was called on, so we have to assume
                                    // that the method may have requirements which we can't fullfil -> warn.
                                    reflectionContext.RecordUnrecognizedPattern(
                                        (int)DiagnosticId.MakeGenericMethod,
                                        new DiagnosticString(DiagnosticId.MakeGenericMethod).GetMessage(
                                            DiagnosticUtilities.GetMethodSignatureDisplayName(calledMethod)));
                                }
                            }
                            // MakeGenericMethod doesn't change the identity of the MethodBase we're tracking so propagate to the return value
                            methodReturnValue = methodParams[0];

                            CheckAndReportRequires(calledMethod, new MessageOrigin(callingMethodBody, offset), RequiresDynamicCodeAttribute);
                        }
                        break;

                    default:
                        if (calledMethod.IsPInvoke)
                        {
                            // Is the PInvoke dangerous?
                            ParameterMetadata[] paramMetadata = calledMethod.GetParameterMetadata();

                            ParameterMetadata returnParamMetadata = Array.Find(paramMetadata, m => m.Index == 0);

                            bool comDangerousMethod = IsComInterop(returnParamMetadata.MarshalAsDescriptor, calledMethod.Signature.ReturnType);
                            for (int paramIndex = 0; paramIndex < calledMethod.Signature.Length; paramIndex++)
                            {
                                MarshalAsDescriptor marshalAsDescriptor = null;
                                for (int metadataIndex = 0; metadataIndex < paramMetadata.Length; metadataIndex++)
                                {
                                    if (paramMetadata[metadataIndex].Index == paramIndex + 1)
                                        marshalAsDescriptor = paramMetadata[metadataIndex].MarshalAsDescriptor;
                                }

                                comDangerousMethod |= IsComInterop(marshalAsDescriptor, calledMethod.Signature[paramIndex]);
                            }

                            if (comDangerousMethod)
                            {
                                reflectionContext.AnalyzingPattern();
                                reflectionContext.RecordUnrecognizedPattern(
                                    (int)DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, 
                                    new DiagnosticString(DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed).GetMessage(DiagnosticUtilities.GetMethodSignatureDisplayName(calledMethod)));
                            }
                        }

                        if (requiresDataFlowAnalysis)
                        {
                            reflectionContext.AnalyzingPattern();
                            for (int parameterIndex = 0; parameterIndex < methodParams.Count; parameterIndex++)
                            {
                                var requiredMemberTypes = _annotations.GetParameterAnnotation(calledMethod, parameterIndex);
                                if (requiredMemberTypes != 0)
                                {
                                    Origin targetContext = DiagnosticUtilities.GetMethodParameterFromIndex(calledMethod, parameterIndex);
                                    RequireDynamicallyAccessedMembers(ref reflectionContext, requiredMemberTypes, methodParams[parameterIndex], targetContext);
                                }
                            }

                            reflectionContext.RecordHandledPattern();
                        }

                        var origin = new MessageOrigin(callingMethodBody, offset);
                        CheckAndReportRequires(calledMethod, origin, RequiresUnreferencedCodeAttribute);
                        CheckAndReportRequires(calledMethod, origin, RequiresDynamicCodeAttribute);
                        CheckAndReportRequires(calledMethod, origin, RequiresAssemblyFilesAttribute);

                        // To get good reporting of errors we need to track the origin of the value for all method calls
                        // but except Newobj as those are special.
                        if (!calledMethod.Signature.ReturnType.IsVoid)
                        {
                            methodReturnValue = new MethodReturnValue(calledMethod, returnValueDynamicallyAccessedMemberTypes);

                            return true;
                        }

                        return false;
                }
            }
            finally
            {
                reflectionContext.Dispose();
            }

            // If we get here, we handled this as an intrinsic.  As a convenience, if the code above
            // didn't set the return value (and the method has a return value), we will set it to be an
            // unknown value with the return type of the method.
            if (methodReturnValue == null)
            {
                if (!calledMethod.Signature.ReturnType.IsVoid)
                {
                    methodReturnValue = new MethodReturnValue(calledMethod, returnValueDynamicallyAccessedMemberTypes);
                }
            }

            // Validate that the return value has the correct annotations as per the method return value annotations
            if (returnValueDynamicallyAccessedMemberTypes != 0)
            {
                foreach (var uniqueValue in methodReturnValue.UniqueValues())
                {
                    if (uniqueValue is LeafValueWithDynamicallyAccessedMemberNode methodReturnValueWithMemberTypes)
                    {
                        if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag(returnValueDynamicallyAccessedMemberTypes))
                            throw new InvalidOperationException($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName()} to {calledMethod.GetDisplayName()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
                    }
                    else if (uniqueValue is SystemTypeValue)
                    {
                        // SystemTypeValue can fullfill any requirement, so it's always valid
                        // The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
                    }
                    else
                    {
                        throw new InvalidOperationException($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName()} to {calledMethod.GetDisplayName()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
                    }
                }
            }

            return true;
        }

        bool IsComInterop(MarshalAsDescriptor marshalInfoProvider, TypeDesc parameterType)
        {
            // This is best effort. One can likely find ways how to get COM without triggering these alarms.
            // AsAny marshalling of a struct with an object-typed field would be one, for example.

            // This logic roughly corresponds to MarshalInfo::MarshalInfo in CoreCLR,
            // not trying to handle invalid cases and distinctions that are not interesting wrt
            // "is this COM?" question.

            NativeTypeKind nativeType = NativeTypeKind.Default;
            if (marshalInfoProvider != null)
            {
                nativeType = marshalInfoProvider.Type;
            }

            if (nativeType == NativeTypeKind.IUnknown || nativeType == NativeTypeKind.IDispatch || nativeType == NativeTypeKind.Intf)
            {
                // This is COM by definition
                return true;
            }

            if (nativeType == NativeTypeKind.Default)
            {
                TypeSystemContext context = parameterType.Context;

                if (parameterType.IsPointer)
                    return false;

                while (parameterType.IsParameterizedType)
                    parameterType = ((ParameterizedType)parameterType).ParameterType;

                if (parameterType.IsWellKnownType(Internal.TypeSystem.WellKnownType.Array))
                {
                    // System.Array marshals as IUnknown by default
                    return true;
                }
                else if (parameterType.IsWellKnownType(Internal.TypeSystem.WellKnownType.String) ||
                    InteropTypes.IsStringBuilder(context, parameterType))
                {
                    // String and StringBuilder are special cased by interop
                    return false;
                }

                if (parameterType.IsValueType)
                {
                    // Value types don't marshal as COM
                    return false;
                }
                else if (parameterType.IsInterface)
                {
                    // Interface types marshal as COM by default
                    return true;
                }
                else if (parameterType.IsDelegate || parameterType.IsWellKnownType(Internal.TypeSystem.WellKnownType.MulticastDelegate)
                    || parameterType == context.GetWellKnownType(Internal.TypeSystem.WellKnownType.MulticastDelegate).BaseType)
                {
                    // Delegates are special cased by interop
                    return false;
                }
                else if (InteropTypes.IsCriticalHandle(context, parameterType))
                {
                    // Subclasses of CriticalHandle are special cased by interop
                    return false;
                }
                else if (InteropTypes.IsSafeHandle(context, parameterType))
                {
                    // Subclasses of SafeHandle are special cased by interop
                    return false;
                }
                else if (parameterType is MetadataType mdType && !mdType.IsSequentialLayout && !mdType.IsExplicitLayout)
                {
                    // Rest of classes that don't have layout marshal as COM
                    return true;
                }
            }

            return false;
        }

        void CheckAndReportRequiresAttributes(bool shouldEnableReflectionWarnings, bool shouldEnableAotWarnings, MethodIL callingMethodBody, int offset, MethodDesc calledMethod)
        {
            if (shouldEnableReflectionWarnings &&
                calledMethod.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "RequiresUnreferencedCodeAttribute"))
            {
                string arg1 = MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage(calledMethod, "RequiresUnreferencedCodeAttribute"));
                string arg2 = MessageFormat.FormatRequiresAttributeUrlArg(DiagnosticUtilities.GetRequiresAttributeUrl(calledMethod, "RequiresUnreferencedCodeAttribute"));

                _logger.LogWarning(callingMethodBody, offset, DiagnosticId.RequiresUnreferencedCode, calledMethod.GetDisplayName(), arg1, arg2);
            }

            if (shouldEnableAotWarnings &&
                calledMethod.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "RequiresDynamicCodeAttribute"))
            {
                LogDynamicCodeWarning(_logger, callingMethodBody, offset, calledMethod);
            }

            static void LogDynamicCodeWarning(Logger logger, MethodIL callingMethodBody, int offset, MethodDesc calledMethod)
            {
                string arg1 = MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage(calledMethod, "RequiresDynamicCodeAttribute"));
                string arg2 = MessageFormat.FormatRequiresAttributeUrlArg(DiagnosticUtilities.GetRequiresAttributeUrl(calledMethod, "RequiresDynamicCodeAttribute"));

                logger.LogWarning(callingMethodBody, offset, DiagnosticId.RequiresDynamicCode, calledMethod.GetDisplayName(), arg1, arg2);
            }
        }

        private bool AnalyzeGenericInstantiationTypeArray(ValueNode arrayParam, ref ReflectionPatternContext reflectionContext, MethodDesc calledMethod, Instantiation genericParameters)
        {
            bool hasRequirements = false;
            foreach (GenericParameterDesc genericParameter in genericParameters)
            {
                if (_annotations.GetGenericParameterAnnotation(genericParameter) != DynamicallyAccessedMemberTypes.None)
                {
                    hasRequirements = true;
                    break;
                }
            }

            // If there are no requirements, then there's no point in warning
            if (!hasRequirements)
                return true;

            foreach (var typesValue in arrayParam.UniqueValues())
            {
                if (typesValue.Kind != ValueNodeKind.Array)
                {
                    return false;
                }
                ArrayValue array = (ArrayValue)typesValue;
                int? size = array.Size.AsConstInt();
                if (size == null || size != genericParameters.Length)
                {
                    return false;
                }
                bool allIndicesKnown = true;
                for (int i = 0; i < size.Value; i++)
                {
                    if (!array.IndexValues.TryGetValue(i, out ValueBasicBlockPair value) || value.Value is null or { Kind: ValueNodeKind.Unknown })
                    {
                        allIndicesKnown = false;
                        break;
                    }
                }

                if (!allIndicesKnown)
                {
                    return false;
                }

                for (int i = 0; i < size.Value; i++)
                {
                    if (array.IndexValues.TryGetValue(i, out ValueBasicBlockPair value))
                    {
                        RequireDynamicallyAccessedMembers(
                            ref reflectionContext,
                            _annotations.GetGenericParameterAnnotation((GenericParameterDesc)genericParameters[i]),
                            value.Value,
                            new MethodOrigin(calledMethod));
                    }
                }
            }
            return true;
        }

        void RequireDynamicallyAccessedMembers(in DiagnosticContext diagnosticContext, in MultiValue value, ValueWithDynamicallyAccessedMembers targetValue, Origin memberWithRequirements)
        {
            var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(_reflectionMarker, diagnosticContext, memberWithRequirements);
            requireDynamicallyAccessedMembersAction.Invoke(value, targetValue);
        }

        void ValidateGenericMethodInstantiation(
            ref ReflectionPatternContext reflectionContext,
            MethodDesc genericMethod,
            ValueNode genericParametersArray,
            MethodDesc reflectionMethod)
        {
            if (!genericMethod.HasInstantiation)
            {
                reflectionContext.RecordHandledPattern();
                return;
            }
            if (!AnalyzeGenericInstantiationTypeArray(genericParametersArray, ref reflectionContext, reflectionMethod, genericMethod.GetMethodDefinition().Instantiation))
            {
                reflectionContext.RecordUnrecognizedPattern(
                    (int)DiagnosticId.MakeGenericMethod,
                    new DiagnosticString(DiagnosticId.MakeGenericMethod).GetMessage(DiagnosticUtilities.GetMethodSignatureDisplayName(reflectionMethod)));
            }
            else
            {
                reflectionContext.RecordHandledPattern();
            }
        }
    }
}
