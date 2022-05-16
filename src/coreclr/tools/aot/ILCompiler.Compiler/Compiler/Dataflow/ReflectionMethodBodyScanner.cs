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

        static bool ShouldSuppressAnalysisWarningsForRequires(TypeSystemEntity originMember, string requiresAttribute)
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
            _reflectionMarker = new ReflectionMarker(logger, factory, annotations, purpose == ScanningPurpose.GetTypeDataflow, ????);
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
                    var origin = new MethodReturnOrigin(method);
                    var diagnosticContext = new DiagnosticContext(new MessageOrigin(method), !ShouldSuppressAnalysisWarningsForRequires(method, RequiresUnreferencedCodeAttribute), scanner._logger);
                    scanner.RequireDynamicallyAccessedMembers(diagnosticContext, scanner.ReturnValue, methodReturnValue);
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
                        RequireDynamicallyAccessedMembers(diagnosticContext, value, parameterValue);
                    }
                }
            }

            // Named arguments next
            TypeDesc attributeType = method.OwningType;
            foreach (var namedArgument in arguments.NamedArguments)
            {
                DynamicallyAccessedMemberTypes annotation = DynamicallyAccessedMemberTypes.None;
                MultiValue targetValues = new();
                if (namedArgument.Kind == CustomAttributeNamedArgumentKind.Field)
                {
                    FieldDesc field = attributeType.GetField(namedArgument.Name);
                    if (field != null)
                    {
                        targetValues = GetFieldValue(field, annotations);
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
                        scanner.RequireDynamicallyAccessedMembers(diagnosticContext, valueNode, targetValue);
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
            var scanner = new ReflectionMethodBodyScanner(factory, flowAnnotations, logger, ScanningPurpose.GetTypeDataflow);
            ReflectionPatternContext reflectionPatternContext = new ReflectionPatternContext(logger, reportingEnabled: true, type, new TypeOrigin(type));
            reflectionPatternContext.AnalyzingPattern();
            scanner.MarkTypeForDynamicallyAccessedMembers(ref reflectionPatternContext, type, annotation);
            reflectionPatternContext.RecordHandledPattern();
            reflectionPatternContext.Dispose();
            return scanner._reflectionMarker.Dependencies;
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
            scanner.RequireDynamicallyAccessedMembers(diagnosticContext, genericArgumentValue, genericParameterValue);

            return scanner._reflectionMarker.Dependencies;
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
                RequireDynamicallyAccessedMembers(diagnosticContext, valueToStore, field);
            }
            CheckAndReportRequires(field, new MessageOrigin(methodBody.OwningMethod), RequiresUnreferencedCodeAttribute);
            CheckAndReportRequires(field, new MessageOrigin(methodBody.OwningMethod), RequiresDynamicCodeAttribute);
        }

        protected override void HandleStoreParameter(MethodIL method, int offset, MethodParameterValue parameter, MultiValue valueToStore)
        {
            if (parameter.DynamicallyAccessedMemberTypes != 0)
            {
                var diagnosticContext = new DiagnosticContext(new MessageOrigin(method, offset), !ShouldSuppressAnalysisWarningsForRequires(method.OwningMethod, RequiresUnreferencedCodeAttribute), _logger);
                RequireDynamicallyAccessedMembers(diagnosticContext, valueToStore, parameter);
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
                var handleCallAction = new HandleCallAction(_context, _reflectionMarker, diagnosticContext, callingMethodDefinition);

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
                            if (calledMethodDefinition.IsPInvokeImpl)
                            {
                                // Is the PInvoke dangerous?
                                bool comDangerousMethod = IsComInterop(calledMethodDefinition.MethodReturnType, calledMethodDefinition.ReturnType);
                                foreach (ParameterDefinition pd in calledMethodDefinition.Parameters)
                                {
                                    comDangerousMethod |= IsComInterop(pd, pd.ParameterType);
                                }

                                if (comDangerousMethod)
                                {
                                    diagnosticContext.AddDiagnostic(DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethodDefinition.GetDisplayName());
                                }
                            }
                            _markStep.CheckAndReportRequiresUnreferencedCode(calledMethodDefinition, _origin);

                            var instanceValue = MultiValueLattice.Top;
                            IReadOnlyList<MultiValue> parameterValues = methodParams;
                            if (calledMethodDefinition.HasImplicitThis())
                            {
                                instanceValue = methodParams[0];
                                parameterValues = parameterValues.Skip(1).ToImmutableList();
                            }
                            return handleCallAction.Invoke(calledMethodDefinition, instanceValue, parameterValues, out methodReturnValue, out _);
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


#if false
                    // TODO: niche APIs that we probably shouldn't even have added
                    //
                    // System.Activator
                    // 
                    // static CreateInstance (string assemblyName, string typeName)
                    // static CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes)
                    // static CreateInstance (string assemblyName, string typeName, object?[]? activationAttributes)
                    //
                    case IntrinsicId.Activator_CreateInstance_AssemblyName_TypeName:
                        ProcessCreateInstanceByName(ref reflectionContext, calledMethod, methodParams);
                        break;

                    //
                    // System.Activator
                    // 
                    // static CreateInstanceFrom (string assemblyFile, string typeName)
                    // static CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
                    // static CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
                    //
                    case IntrinsicId.Activator_CreateInstanceFrom:
                        ProcessCreateInstanceByName(ref reflectionContext, calledMethod, methodParams);
                        break;
#endif

#if false
                    // TODO: niche APIs that we probably shouldn't even have added
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
                        ProcessCreateInstanceByName(ref reflectionContext, calledMethod, methodParams);
                        break;
#endif

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

                if (parameterType.IsWellKnownType(WellKnownType.Array))
                {
                    // System.Array marshals as IUnknown by default
                    return true;
                }
                else if (parameterType.IsWellKnownType(WellKnownType.String) ||
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
                else if (parameterType.IsDelegate || parameterType.IsWellKnownType(WellKnownType.MulticastDelegate)
                    || parameterType == context.GetWellKnownType(WellKnownType.MulticastDelegate).BaseType)
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

#if false
        void ProcessCreateInstanceByName(ref ReflectionPatternContext reflectionContext, MethodDesc calledMethod, ValueNodeList methodParams)
        {
            reflectionContext.AnalyzingPattern();

            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            bool parameterlessConstructor = true;
            if (calledMethod.Parameters.Count == 8 && calledMethod.Parameters[2].ParameterType.MetadataType == MetadataType.Boolean)
            {
                parameterlessConstructor = false;
                bindingFlags = BindingFlags.Instance;
				if (methodParams[3].AsConstInt() is int bindingFlagsInt)
					bindingFlags |= (BindingFlags)bindingFlagsInt;
				else
					bindingFlags |= BindingFlags.Public | BindingFlags.NonPublic;
            }

            int methodParamsOffset = !calledMethod.Signature.IsStatic ? 1 : 0;

            foreach (var assemblyNameValue in methodParams[methodParamsOffset].UniqueValues())
            {
                if (assemblyNameValue is KnownStringValue assemblyNameStringValue)
                {
                    foreach (var typeNameValue in methodParams[methodParamsOffset + 1].UniqueValues())
                    {
                        if (typeNameValue is KnownStringValue typeNameStringValue)
                        {
                            var resolvedAssembly = _context.GetLoadedAssembly(assemblyNameStringValue.Contents);
                            if (resolvedAssembly == null)
                            {
                                reflectionContext.RecordUnrecognizedPattern(2061, $"The assembly name '{assemblyNameStringValue.Contents}' passed to method '{calledMethod.GetDisplayName()}' references assembly which is not available.");
                                continue;
                            }

                            var resolvedType = _context.TypeNameResolver.ResolveTypeName(resolvedAssembly, typeNameStringValue.Contents)?.Resolve();
                            if (resolvedType == null)
                            {
                                // It's not wrong to have a reference to non-existing type - the code may well expect to get an exception in this case
                                // Note that we did find the assembly, so it's not a linker config problem, it's either intentional, or wrong versions of assemblies
                                // but linker can't know that.
                                reflectionContext.RecordHandledPattern();
                                continue;
                            }

                            MarkConstructorsOnType(ref reflectionContext, resolvedType, parameterlessConstructor ? m => m.Parameters.Count == 0 : null, bindingFlags);
                        }
                        else
                        {
                            reflectionContext.RecordUnrecognizedPattern(2032, $"Unrecognized value passed to the parameter '{calledMethod.Parameters[1].Name}' of method '{calledMethod.GetDisplayName()}'. It's not possible to guarantee the availability of the target type.");
                        }
                    }
                }
                else
                {
                    reflectionContext.RecordUnrecognizedPattern(2032, $"Unrecognized value passed to the parameter '{calledMethod.Parameters[0].Name}' of method '{calledMethod.GetDisplayName()}'. It's not possible to guarantee the availability of the target type.");
                }
            }
        }
#endif

        void ProcessGetMethodByName(
            ref ReflectionPatternContext reflectionContext,
            TypeDesc typeDefinition,
            string methodName,
            BindingFlags? bindingFlags,
            ref ValueNode methodReturnValue)
        {
            bool foundAny = false;
            foreach (var method in typeDefinition.GetMethodsOnTypeHierarchy(m => m.Name == methodName, bindingFlags))
            {
                MarkMethod(ref reflectionContext, method);
                methodReturnValue = MergePointValue.MergeValues(methodReturnValue, new SystemReflectionMethodBaseValue(method));
                foundAny = true;
            }
            // If there were no methods found the API will return null at runtime, so we should
            // track the null as a return value as well.
            // This also prevents warnings in such case, since if we don't set the return value it will be
            // "unknown" and consumers may warn.
            if (!foundAny)
                methodReturnValue = MergePointValue.MergeValues(methodReturnValue, NullValue.Instance);
        }

        public static DynamicallyAccessedMemberTypes GetMissingMemberTypes(DynamicallyAccessedMemberTypes requiredMemberTypes, DynamicallyAccessedMemberTypes availableMemberTypes)
        {
            if (availableMemberTypes.HasFlag(requiredMemberTypes))
                return DynamicallyAccessedMemberTypes.None;

            if (requiredMemberTypes == DynamicallyAccessedMemberTypes.All)
                return DynamicallyAccessedMemberTypes.All;

            var missingMemberTypes = requiredMemberTypes & ~availableMemberTypes;

            // PublicConstructors is a special case since its value is 3 - so PublicParameterlessConstructor (1) | _PublicConstructor_WithMoreThanOneParameter_ (2)
            // The above bit logic only works for value with single bit set.
            if (requiredMemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicConstructors) &&
                !availableMemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicConstructors))
                missingMemberTypes |= DynamicallyAccessedMemberTypes.PublicConstructors;

            return missingMemberTypes;
        }

        private string GetMemberTypesString(DynamicallyAccessedMemberTypes memberTypes)
        {
            Debug.Assert(memberTypes != DynamicallyAccessedMemberTypes.None);

            if (memberTypes == DynamicallyAccessedMemberTypes.All)
                return $"'{nameof(DynamicallyAccessedMemberTypes)}.{nameof(DynamicallyAccessedMemberTypes.All)}'";

            var memberTypesList = Enum.GetValues<DynamicallyAccessedMemberTypes>()
                .Where(damt => (memberTypes & damt) == damt && damt != DynamicallyAccessedMemberTypes.None)
                .ToList();

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicConstructors))
                memberTypesList.Remove(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);

            return string.Join(", ", memberTypesList.Select(mt => $"'{nameof(DynamicallyAccessedMemberTypes)}.{mt}'"));
        }

        void RequireDynamicallyAccessedMembers(ref ReflectionPatternContext reflectionContext, DynamicallyAccessedMemberTypes requiredMemberTypes, ValueNode value, Origin targetContext)
        {
            foreach (var uniqueValue in value.UniqueValues())
            {
                if (requiredMemberTypes == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
                    && uniqueValue is SystemTypeForGenericParameterValue genericParam
                    && genericParam.GenericParameter.HasDefaultConstructorConstraint)
                {
                    // We allow a new() constraint on a generic parameter to satisfy DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
                    reflectionContext.RecordHandledPattern();
                }
                else if (uniqueValue is LeafValueWithDynamicallyAccessedMemberNode valueWithDynamicallyAccessedMember)
                {
                    var availableMemberTypes = valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes;
                    var missingMemberTypesValue = GetMissingMemberTypes(requiredMemberTypes, availableMemberTypes);
                    if (missingMemberTypesValue != DynamicallyAccessedMemberTypes.None)
                    {
                        var missingMemberTypes = GetMemberTypesString(missingMemberTypesValue);
                        switch ((valueWithDynamicallyAccessedMember.SourceContext, targetContext))
                        {
                            case (ParameterOrigin sourceParameter, ParameterOrigin targetParameter):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter).GetMessage(
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(targetParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetParameter.Method),
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(sourceParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceParameter.Method),
                                    missingMemberTypes));
                                break;
                            case (ParameterOrigin sourceParameter, MethodReturnOrigin targetMethodReturnType):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType).GetMessage(
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetMethodReturnType.Method),
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(sourceParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceParameter.Method),
                                    missingMemberTypes));
                                break;
                            case (ParameterOrigin sourceParameter, FieldOrigin targetField):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField).GetMessage(
                                    targetField.GetDisplayName(),
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(sourceParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceParameter.Method),
                                    missingMemberTypes));
                                break;
                            case (ParameterOrigin sourceParameter, MethodOrigin targetMethod):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter).GetMessage(
                                    targetMethod.GetDisplayName(),
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(sourceParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceParameter.Method),
                                    missingMemberTypes));
                                break;
                            case (ParameterOrigin sourceParameter, GenericParameterOrigin targetGenericParameter):
                                // Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsGenericParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsGenericParameter).GetMessage(
                                    targetGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(targetGenericParameter),
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(sourceParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceParameter.Method),
                                    missingMemberTypes));
                                break;

                            case (MethodReturnOrigin sourceMethodReturnType, ParameterOrigin targetParameter):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter).GetMessage(
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(targetParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetParameter.Method),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceMethodReturnType.Method),
                                    missingMemberTypes));
                                break;
                            case (MethodReturnOrigin sourceMethodReturnType, MethodReturnOrigin targetMethodReturnType):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType).GetMessage(
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetMethodReturnType.Method),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceMethodReturnType.Method),
                                    missingMemberTypes));
                                break;
                            case (MethodReturnOrigin sourceMethodReturnType, FieldOrigin targetField):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField).GetMessage(
                                    targetField.GetDisplayName(),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceMethodReturnType.Method),
                                    missingMemberTypes));
                                break;
                            case (MethodReturnOrigin sourceMethodReturnType, MethodOrigin targetMethod):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter).GetMessage(
                                    targetMethod.GetDisplayName(),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceMethodReturnType.Method),
                                    missingMemberTypes));
                                break;
                            case (MethodReturnOrigin sourceMethodReturnType, GenericParameterOrigin targetGenericParameter):
                                // Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsGenericParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsGenericParameter).GetMessage(
                                    targetGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(targetGenericParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(sourceMethodReturnType.Method),
                                    missingMemberTypes));
                                break;

                            case (FieldOrigin sourceField, ParameterOrigin targetParameter):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter).GetMessage(
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(targetParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetParameter.Method),
                                    sourceField.GetDisplayName(),
                                    missingMemberTypes));
                                break;
                            case (FieldOrigin sourceField, MethodReturnOrigin targetMethodReturnType):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType).GetMessage(
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetMethodReturnType.Method),
                                    sourceField.GetDisplayName(),
                                    missingMemberTypes));
                                break;
                            case (FieldOrigin sourceField, FieldOrigin targetField):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField).GetMessage(
                                    targetField.GetDisplayName(),
                                    sourceField.GetDisplayName(),
                                    missingMemberTypes));
                                break;
                            case (FieldOrigin sourceField, MethodOrigin targetMethod):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter).GetMessage(
                                    targetMethod.GetDisplayName(),
                                    sourceField.GetDisplayName(),
                                    missingMemberTypes));
                                break;
                            case (FieldOrigin sourceField, GenericParameterOrigin targetGenericParameter):
                                // Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter).GetMessage(
                                    targetGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(targetGenericParameter),
                                    sourceField.GetDisplayName(),
                                    missingMemberTypes));
                                break;

                            case (MethodOrigin sourceMethod, ParameterOrigin targetParameter):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter).GetMessage(
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(targetParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetParameter.Method),
                                    sourceMethod.GetDisplayName(),
                                    missingMemberTypes));
                                break;
                            case (MethodOrigin sourceMethod, MethodReturnOrigin targetMethodReturnType):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType).GetMessage(
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetMethodReturnType.Method),
                                    sourceMethod.GetDisplayName(),
                                    missingMemberTypes));
                                break;
                            case (MethodOrigin sourceMethod, FieldOrigin targetField):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField).GetMessage(
                                    targetField.GetDisplayName(),
                                    sourceMethod.GetDisplayName(),
                                    missingMemberTypes));
                                break;
                            case (MethodOrigin sourceMethod, MethodOrigin targetMethod):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter).GetMessage(
                                    targetMethod.GetDisplayName(),
                                    sourceMethod.GetDisplayName(),
                                    missingMemberTypes));
                                break;
                            case (MethodOrigin sourceMethod, GenericParameterOrigin targetGenericParameter):
                                // Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsGenericParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsGenericParameter).GetMessage(
                                    targetGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(targetGenericParameter),
                                    sourceMethod.GetDisplayName(),
                                    missingMemberTypes));
                                break;

                            case (GenericParameterOrigin sourceGenericParameter, ParameterOrigin targetParameter):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter).GetMessage(
                                    DiagnosticUtilities.GetParameterNameForErrorMessage(targetParameter),
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetParameter.Method),
                                    sourceGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(sourceGenericParameter),
                                    missingMemberTypes));
                                break;
                            case (GenericParameterOrigin sourceGenericParameter, MethodReturnOrigin targetMethodReturnType):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType).GetMessage(
                                    DiagnosticUtilities.GetMethodSignatureDisplayName(targetMethodReturnType.Method),
                                    sourceGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(sourceGenericParameter),
                                    missingMemberTypes));
                                break;
                            case (GenericParameterOrigin sourceGenericParameter, FieldOrigin targetField):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField).GetMessage(
                                    targetField.GetDisplayName(),
                                    sourceGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(sourceGenericParameter),
                                    missingMemberTypes));
                                break;
                            case (GenericParameterOrigin sourceGenericParameter, MethodOrigin targetMethod):
                                // Currently this is never generated, it might be possible one day if we try to validate annotations on results of reflection
                                // For example code like this should ideally one day generate the warning
                                // void TestMethod<T>()
                                // {
                                //    // This passes the T as the "this" parameter to Type.GetMethods()
                                //    typeof(Type).GetMethod("GetMethods").Invoke(typeof(T), new object[] {});
                                // }
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter).GetMessage(
                                    targetMethod.GetDisplayName(),
                                    sourceGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(sourceGenericParameter),
                                    missingMemberTypes));
                                break;
                            case (GenericParameterOrigin sourceGenericParameter, GenericParameterOrigin targetGenericParameter):
                                reflectionContext.RecordUnrecognizedPattern((int)DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter,
                                    new DiagnosticString(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter).GetMessage(
                                    targetGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(targetGenericParameter),
                                    sourceGenericParameter.Name,
                                    DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(sourceGenericParameter),
                                    missingMemberTypes));
                                break;

                            default:
                                throw new NotImplementedException($"unsupported source context {valueWithDynamicallyAccessedMember.SourceContext} or target context {targetContext}");
                        };
                    }
                    else
                    {
                        reflectionContext.RecordHandledPattern();
                    }
                }
                else if (uniqueValue is SystemTypeValue systemTypeValue)
                {
                    MarkTypeForDynamicallyAccessedMembers(ref reflectionContext, systemTypeValue.TypeRepresented, requiredMemberTypes);
                }
                else if (uniqueValue is KnownStringValue knownStringValue)
                {
                    ModuleDesc callingModule = ((reflectionContext.Source as MethodDesc)?.OwningType as MetadataType)?.Module;

                    if (!ILCompiler.DependencyAnalysis.ReflectionMethodBodyScanner.ResolveType(knownStringValue.Contents, callingModule, reflectionContext.Source.Context, out TypeDesc foundType, out ModuleDesc referenceModule))
                    {
                        // Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
                        reflectionContext.RecordHandledPattern();
                    }
                    else
                    {
                        // Also add module metadata in case this reference was through a type forward
                        if (_factory.MetadataManager.CanGenerateMetadata(referenceModule.GetGlobalModuleType()))
                            _reflectionMarker.Dependencies.Add(_factory.ModuleMetadata(referenceModule), reflectionContext.MemberWithRequirements.ToString());

                        MarkType(ref reflectionContext, foundType);
                        MarkTypeForDynamicallyAccessedMembers(ref reflectionContext, foundType, requiredMemberTypes);
                    }
                }
                else if (uniqueValue == NullValue.Instance)
                {
                    // Ignore - probably unreachable path as it would fail at runtime anyway.
                }
                else
                {
                    switch (targetContext)
                    {
                        case ParameterOrigin parameterDefinition:
                            reflectionContext.RecordUnrecognizedPattern(
                                2062,
                                $"Value passed to parameter '{DiagnosticUtilities.GetParameterNameForErrorMessage(parameterDefinition)}' of method '{DiagnosticUtilities.GetMethodSignatureDisplayName(parameterDefinition.Method)}' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
                            break;
                        case MethodReturnOrigin methodReturnType:
                            reflectionContext.RecordUnrecognizedPattern(
                                2063,
                                $"Value returned from method '{DiagnosticUtilities.GetMethodSignatureDisplayName(methodReturnType.Method)}' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
                            break;
                        case FieldOrigin fieldDefinition:
                            reflectionContext.RecordUnrecognizedPattern(
                                2064,
                                $"Value assigned to {fieldDefinition.GetDisplayName()} can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
                            break;
                        case MethodOrigin methodDefinition:
                            reflectionContext.RecordUnrecognizedPattern(
                                2065,
                                $"Value passed to implicit 'this' parameter of method '{methodDefinition.GetDisplayName()}' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
                            break;
                        case GenericParameterOrigin genericParameter:
                            // Unknown value to generic parameter - this is possible if the generic argumnet fails to resolve
                            reflectionContext.RecordUnrecognizedPattern(
                                2066,
                                $"Type passed to generic parameter '{genericParameter.Name}' of '{DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(genericParameter)}' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
                            break;
                        default: throw new NotImplementedException($"unsupported target context {targetContext.GetType()}");
                    };
                }
            }

            reflectionContext.RecordHandledPattern();
        }

        static BindingFlags? GetBindingFlagsFromValue(ValueNode parameter) => (BindingFlags?)parameter.AsConstInt();

        static bool BindingFlagsAreUnsupported(BindingFlags? bindingFlags)
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

        static bool HasBindingFlag(BindingFlags? bindingFlags, BindingFlags? search) => bindingFlags != null && (bindingFlags & search) == search;

        void MarkTypeForDynamicallyAccessedMembers(ref ReflectionPatternContext reflectionContext, TypeDesc typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, bool declaredOnly = false)
        {
            foreach (var member in typeDefinition.GetDynamicallyAccessedMembers(requiredMemberTypes, declaredOnly))
            {
                switch (member)
                {
                    case MethodDesc method:
                        MarkMethod(ref reflectionContext, method);
                        break;
                    case FieldDesc field:
                        MarkField(ref reflectionContext, field);
                        break;
                    case MetadataType type:
                        MarkType(ref reflectionContext, type);
                        break;
                    case PropertyPseudoDesc property:
                        MarkProperty(ref reflectionContext, property);
                        break;
                    case EventPseudoDesc @event:
                        MarkEvent(ref reflectionContext, @event);
                        break;
                    default:
                        Debug.Fail(member.GetType().ToString());
                        break;
                }
            }
        }

        void MarkType(ref ReflectionPatternContext reflectionContext, TypeDesc type)
        {
            RootingHelpers.TryGetDependenciesForReflectedType(ref _reflectionMarker.Dependencies, _factory, type, reflectionContext.MemberWithRequirements.ToString());
            reflectionContext.RecordHandledPattern();
        }

        void WarnOnReflectionAccess(ref ReflectionPatternContext context, TypeSystemEntity entity)
        {
            if (_purpose == ScanningPurpose.GetTypeDataflow)
            {
                // Don't check whether the current scope is a RUC type or RUC method because these warnings
                // are not suppressed in RUC scopes. Here the scope represents the DynamicallyAccessedMembers
                // annotation on a type, not a callsite which uses the annotation. We always want to warn about
                // possible reflection access indicated by these annotations.
                _logger.LogWarning(context.Source, DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithDynamicallyAccessedMembers,
                    ((TypeOrigin)context.MemberWithRequirements).GetDisplayName(), entity.GetDisplayName());
            }
            else
            {
                if (entity is FieldDesc && context.ReportingEnabled)
                {
                    _logger.LogWarning(context.Source, DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection, entity.GetDisplayName());
                }
                else
                {
                    Debug.Assert(entity is MethodDesc);

                    _logger.LogWarning(context.Source, DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, entity.GetDisplayName());
                }
            }
        }

        void MarkMethod(ref ReflectionPatternContext reflectionContext, MethodDesc method)
        {
            if(method.DoesMethodRequire(RequiresUnreferencedCodeAttribute, out _))
            {
                if (_purpose == ScanningPurpose.GetTypeDataflow)
                {
                    _logger.LogWarning(reflectionContext.Source, DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithRequiresUnreferencedCode,
                        ((TypeOrigin)reflectionContext.MemberWithRequirements).GetDisplayName(), method.GetDisplayName());
                }
            }

            if (_annotations.ShouldWarnWhenAccessedForReflection(method) && !ShouldSuppressAnalysisWarningsForRequires(method, RequiresUnreferencedCodeAttribute))
            {
                WarnOnReflectionAccess(ref reflectionContext, method);
            }

            RootingHelpers.TryGetDependenciesForReflectedMethod(ref _reflectionMarker.Dependencies, _factory, method, reflectionContext.MemberWithRequirements.ToString());
            reflectionContext.RecordHandledPattern();
        }

        void MarkField(ref ReflectionPatternContext reflectionContext, FieldDesc field)
        {
            if (_annotations.ShouldWarnWhenAccessedForReflection(field) && !ShouldSuppressAnalysisWarningsForRequires(reflectionContext.Source, RequiresUnreferencedCodeAttribute))
            {
                WarnOnReflectionAccess(ref reflectionContext, field);
            }

            RootingHelpers.TryGetDependenciesForReflectedField(ref _reflectionMarker.Dependencies, _factory, field, reflectionContext.MemberWithRequirements.ToString());
            reflectionContext.RecordHandledPattern();
        }

        void MarkProperty(ref ReflectionPatternContext reflectionContext, PropertyPseudoDesc property)
        {
            if (property.GetMethod != null)
                MarkMethod(ref reflectionContext, property.GetMethod);
            if (property.SetMethod != null)
                MarkMethod(ref reflectionContext, property.SetMethod);
            reflectionContext.RecordHandledPattern();
        }

        void MarkEvent(ref ReflectionPatternContext reflectionContext, EventPseudoDesc @event)
        {
            if (@event.AddMethod != null)
                MarkMethod(ref reflectionContext, @event.AddMethod);
            if (@event.RemoveMethod != null)
                MarkMethod(ref reflectionContext, @event.RemoveMethod);
            reflectionContext.RecordHandledPattern();
        }

        void MarkConstructorsOnType(ref ReflectionPatternContext reflectionContext, TypeDesc type, Func<MethodDesc, bool> filter, BindingFlags? bindingFlags = null)
        {
            foreach (var ctor in type.GetConstructorsOnType(filter, bindingFlags))
                MarkMethod(ref reflectionContext, ctor);
        }

        void MarkFieldsOnTypeHierarchy(ref ReflectionPatternContext reflectionContext, TypeDesc type, Func<FieldDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            foreach (var field in type.GetFieldsOnTypeHierarchy(filter, bindingFlags))
                MarkField(ref reflectionContext, field);
        }

        MetadataType[] MarkNestedTypesOnType(ref ReflectionPatternContext reflectionContext, TypeDesc type, Func<MetadataType, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            var result = new ArrayBuilder<MetadataType>();

            foreach (var nestedType in type.GetNestedTypesOnType(filter, bindingFlags))
            {
                result.Add(nestedType);
                MarkTypeForDynamicallyAccessedMembers(ref reflectionContext, nestedType, DynamicallyAccessedMemberTypes.All);
            }

            return result.ToArray();
        }

        void MarkPropertiesOnTypeHierarchy(ref ReflectionPatternContext reflectionContext, TypeDesc type, Func<PropertyPseudoDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            foreach (var property in type.GetPropertiesOnTypeHierarchy(filter, bindingFlags))
                MarkProperty(ref reflectionContext, property);
        }

        void MarkEventsOnTypeHierarchy(ref ReflectionPatternContext reflectionContext, TypeDesc type, Func<EventPseudoDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            foreach (var @event in type.GetEventsOnTypeHierarchy(filter, bindingFlags))
                MarkEvent(ref reflectionContext, @event);
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

        static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes(BindingFlags? bindingFlags) =>
            (HasBindingFlag(bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicNestedTypes : DynamicallyAccessedMemberTypes.None) |
            (HasBindingFlag(bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicNestedTypes : DynamicallyAccessedMemberTypes.None) |
            (BindingFlagsAreUnsupported(bindingFlags) ? DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes : DynamicallyAccessedMemberTypes.None);

        static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors(BindingFlags? bindingFlags) =>
            (HasBindingFlag(bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicConstructors : DynamicallyAccessedMemberTypes.None) |
            (HasBindingFlag(bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicConstructors : DynamicallyAccessedMemberTypes.None) |
            (BindingFlagsAreUnsupported(bindingFlags) ? DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors : DynamicallyAccessedMemberTypes.None);

        static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods(BindingFlags? bindingFlags) =>
            (HasBindingFlag(bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicMethods : DynamicallyAccessedMemberTypes.None) |
            (HasBindingFlag(bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicMethods : DynamicallyAccessedMemberTypes.None) |
            (BindingFlagsAreUnsupported(bindingFlags) ? DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods : DynamicallyAccessedMemberTypes.None);

        static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields(BindingFlags? bindingFlags) =>
            (HasBindingFlag(bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicFields : DynamicallyAccessedMemberTypes.None) |
            (HasBindingFlag(bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicFields : DynamicallyAccessedMemberTypes.None) |
            (BindingFlagsAreUnsupported(bindingFlags) ? DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields : DynamicallyAccessedMemberTypes.None);

        static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties(BindingFlags? bindingFlags) =>
            (HasBindingFlag(bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicProperties : DynamicallyAccessedMemberTypes.None) |
            (HasBindingFlag(bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicProperties : DynamicallyAccessedMemberTypes.None) |
            (BindingFlagsAreUnsupported(bindingFlags) ? DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties : DynamicallyAccessedMemberTypes.None);

        static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents(BindingFlags? bindingFlags) =>
            (HasBindingFlag(bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicEvents : DynamicallyAccessedMemberTypes.None) |
            (HasBindingFlag(bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicEvents : DynamicallyAccessedMemberTypes.None) |
            (BindingFlagsAreUnsupported(bindingFlags) ? DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents : DynamicallyAccessedMemberTypes.None);

        static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMembers(BindingFlags? bindingFlags) =>
            GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors(bindingFlags) |
            GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents(bindingFlags) |
            GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields(bindingFlags) |
            GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods(bindingFlags) |
            GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties(bindingFlags) |
            GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes(bindingFlags);
    }
}
