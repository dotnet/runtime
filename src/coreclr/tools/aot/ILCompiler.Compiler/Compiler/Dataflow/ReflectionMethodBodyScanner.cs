// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Logging;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Internal.IL;
using Internal.TypeSystem;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using InteropTypes = Internal.TypeSystem.Interop.InteropTypes;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using NodeFactory = ILCompiler.DependencyAnalysis.NodeFactory;

#nullable enable

namespace ILCompiler.Dataflow
{
    internal sealed class ReflectionMethodBodyScanner : MethodBodyScanner
    {
        private readonly Logger _logger;
        private readonly NodeFactory _factory;
        private ReflectionMarker _reflectionMarker;
        private readonly TrimAnalysisPatternStore TrimAnalysisPatterns;

        private MessageOrigin _origin;

        public static bool RequiresReflectionMethodBodyScannerForCallSite(FlowAnnotations flowAnnotations, MethodDesc method)
        {
            return Intrinsics.GetIntrinsicIdForMethod(method) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
                flowAnnotations.RequiresDataflowAnalysisDueToSignature(method) ||
                GenericArgumentDataFlow.RequiresGenericArgumentDataFlow(flowAnnotations, method) ||
                method.DoesMethodRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _) ||
                method.DoesMethodRequire(DiagnosticUtilities.RequiresAssemblyFilesAttribute, out _) ||
                method.DoesMethodRequire(DiagnosticUtilities.RequiresDynamicCodeAttribute, out _) ||
                IsPInvokeDangerous(method, out _, out _);
        }

        public static bool RequiresReflectionMethodBodyScannerForMethodBody(FlowAnnotations flowAnnotations, MethodDesc methodDefinition)
        {
            return Intrinsics.GetIntrinsicIdForMethod(methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
                flowAnnotations.RequiresDataflowAnalysisDueToSignature(methodDefinition);
        }

        public static bool RequiresReflectionMethodBodyScannerForAccess(FlowAnnotations flowAnnotations, FieldDesc field)
        {
            return flowAnnotations.RequiresDataflowAnalysisDueToSignature(field) ||
                GenericArgumentDataFlow.RequiresGenericArgumentDataFlow(flowAnnotations, field) ||
                field.DoesFieldRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _) ||
                field.DoesFieldRequire(DiagnosticUtilities.RequiresAssemblyFilesAttribute, out _) ||
                field.DoesFieldRequire(DiagnosticUtilities.RequiresDynamicCodeAttribute, out _);
        }

        internal static void CheckAndReportAllRequires(in DiagnosticContext diagnosticContext, TypeSystemEntity calledMember)
        {
            CheckAndReportRequires(diagnosticContext, calledMember, DiagnosticUtilities.RequiresUnreferencedCodeAttribute);
            CheckAndReportRequires(diagnosticContext, calledMember, DiagnosticUtilities.RequiresDynamicCodeAttribute);
            CheckAndReportRequires(diagnosticContext, calledMember, DiagnosticUtilities.RequiresAssemblyFilesAttribute);
        }

        internal static void CheckAndReportRequires(in DiagnosticContext diagnosticContext, TypeSystemEntity calledMember, string requiresAttributeName)
        {
            if (!calledMember.DoesMemberRequire(requiresAttributeName, out var requiresAttribute))
                return;

            ReportRequires(diagnosticContext, calledMember, requiresAttributeName, requiresAttribute.Value);
        }

        internal static void ReportRequires(in DiagnosticContext diagnosticContext, TypeSystemEntity calledMember, string requiresAttributeName, in CustomAttributeValue<TypeDesc> requiresAttribute)
        {
            DiagnosticId diagnosticId = requiresAttributeName switch
            {
                DiagnosticUtilities.RequiresUnreferencedCodeAttribute => DiagnosticId.RequiresUnreferencedCode,
                DiagnosticUtilities.RequiresDynamicCodeAttribute => DiagnosticId.RequiresDynamicCode,
                DiagnosticUtilities.RequiresAssemblyFilesAttribute => DiagnosticId.RequiresAssemblyFiles,
                _ => throw new NotImplementedException($"{requiresAttributeName} is not a valid supported Requires attribute"),
            };

            string arg1 = MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage(requiresAttribute));
            string arg2 = MessageFormat.FormatRequiresAttributeUrlArg(DiagnosticUtilities.GetRequiresAttributeUrl(requiresAttribute));

            diagnosticContext.AddDiagnostic(diagnosticId, calledMember.GetDisplayName(), arg1, arg2);
        }

        private ReflectionMethodBodyScanner(NodeFactory factory, FlowAnnotations annotations, Logger logger, MessageOrigin origin)
            : base(annotations)
        {
            _logger = logger;
            _factory = factory;
            _origin = origin;
            _reflectionMarker = new ReflectionMarker(logger, factory, annotations, typeHierarchyDataFlowOrigin: null, enabled: false);
            TrimAnalysisPatterns = new TrimAnalysisPatternStore(MultiValueLattice, logger);
        }

        public override void InterproceduralScan(MethodIL methodBody)
        {
            base.InterproceduralScan(methodBody);

            // Replace the reflection marker with one which actually marks
            _reflectionMarker = new ReflectionMarker(_logger, _factory, _annotations, typeHierarchyDataFlowOrigin: null, enabled: true);
            TrimAnalysisPatterns.MarkAndProduceDiagnostics(_reflectionMarker);
        }

        protected override void Scan(MethodIL methodBody, ref InterproceduralState interproceduralState)
        {
            _origin = new MessageOrigin(methodBody.OwningMethod);
            base.Scan(methodBody, ref interproceduralState);
        }

        public static DependencyList ScanAndProcessReturnValue(NodeFactory factory, FlowAnnotations annotations, Logger logger, MethodIL methodBody, out List<INodeWithRuntimeDeterminedDependencies> runtimeDependencies)
        {
            var scanner = new ReflectionMethodBodyScanner(factory, annotations, logger, new MessageOrigin(methodBody.OwningMethod));

            scanner.InterproceduralScan(methodBody);

            runtimeDependencies = scanner._reflectionMarker.RuntimeDeterminedDependencies;
            return scanner._reflectionMarker.Dependencies;
        }

        public static DependencyList ProcessTypeGetTypeDataflow(NodeFactory factory, FlowAnnotations flowAnnotations, Logger logger, MetadataType type)
        {
            DynamicallyAccessedMemberTypes annotation = flowAnnotations.GetTypeAnnotation(type);
            Debug.Assert(annotation != DynamicallyAccessedMemberTypes.None);
            var reflectionMarker = new ReflectionMarker(logger, factory, flowAnnotations, typeHierarchyDataFlowOrigin: type, enabled: true);

            // We need to apply annotations to this type, and its base/interface types (recursively)
            // But the annotations on base/interfaces may already be applied so we don't need to apply those
            // again (and should avoid doing so as it would produce extra warnings).
            MessageOrigin origin = new MessageOrigin(type);
            if (type.HasBaseType)
            {
                var baseAnnotation = flowAnnotations.GetTypeAnnotation(type.BaseType);
                var annotationToApplyToBase = Annotations.GetMissingMemberTypes(annotation, baseAnnotation);

                // Apply any annotations that didn't exist on the base type to the base type.
                // This may produce redundant warnings when the annotation is DAMT.All or DAMT.PublicConstructors and the base already has a
                // subset of those annotations.
                reflectionMarker.MarkTypeForDynamicallyAccessedMembers(origin, type.BaseType, annotationToApplyToBase, type.GetDisplayName(), declaredOnly: false);
            }

            // Most of the DynamicallyAccessedMemberTypes don't select members on interfaces. We only need to apply
            // annotations to interfaces separately if dealing with DAMT.All or DAMT.Interfaces.
            if (annotation.HasFlag(DynamicallyAccessedMemberTypes.Interfaces))
            {
                var annotationToApplyToInterfaces = annotation == DynamicallyAccessedMemberTypes.All ? annotation : DynamicallyAccessedMemberTypes.Interfaces;
                foreach (var iface in type.RuntimeInterfaces)
                {
                    if (flowAnnotations.GetTypeAnnotation(iface).HasFlag(annotationToApplyToInterfaces))
                        continue;

                    // Apply All or Interfaces to the interface type.
                    // DAMT.All may produce redundant warnings from implementing types, when the interface type already had some annotations.
                    reflectionMarker.MarkTypeForDynamicallyAccessedMembers(origin, iface, annotationToApplyToInterfaces, type.GetDisplayName(), declaredOnly: false);
                }
            }

            // The annotations this type inherited from its base types or interfaces should not produce
            // warnings on the respective base/interface members, since those are already covered by applying
            // the annotations to those types. So we only need to handle the members directly declared on this type.
            reflectionMarker.MarkTypeForDynamicallyAccessedMembers(new MessageOrigin(type), type, annotation, type.GetDisplayName(), declaredOnly: true);
            return reflectionMarker.Dependencies;
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

        protected override ValueWithDynamicallyAccessedMembers GetMethodParameterValue(ParameterProxy parameter)
            => GetMethodParameterValue(parameter, _annotations.GetParameterAnnotation(parameter));

        private MethodParameterValue GetMethodParameterValue(ParameterProxy parameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
            => _annotations.GetMethodParameterValue(parameter, dynamicallyAccessedMemberTypes);

        protected override MethodReturnValue GetReturnValue(MethodIL method) => _annotations.GetMethodReturnValue(method.OwningMethod, isNewObj: false);

        /// <summary>
        /// HandleGetField is called every time the scanner needs to represent a value of the field
        /// either as a source or target. It is not called when just a reference to field is created,
        /// But if such reference is dereferenced then it will get called.
        /// </summary>
        protected override MultiValue HandleGetField(MethodIL methodBody, int offset, FieldDesc field)
        {
            _origin = _origin.WithInstructionOffset(methodBody, offset);

            if (field.DoesFieldRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _) ||
                field.DoesFieldRequire(DiagnosticUtilities.RequiresDynamicCodeAttribute, out _) ||
                field.DoesFieldRequire(DiagnosticUtilities.RequiresAssemblyFilesAttribute, out _))
                TrimAnalysisPatterns.Add(new TrimAnalysisFieldAccessPattern(field, _origin));

            ProcessGenericArgumentDataFlow(field);

            return _annotations.GetFieldValue(field);
        }

        private void HandleStoreValueWithDynamicallyAccessedMembers(MethodIL methodBody, int offset, ValueWithDynamicallyAccessedMembers targetValue, MultiValue sourceValue, int? parameterIndex, string reason)
        {
            if (targetValue.DynamicallyAccessedMemberTypes != 0)
            {
                _origin = _origin.WithInstructionOffset(methodBody, offset);
                TrimAnalysisPatterns.Add(new TrimAnalysisAssignmentPattern(sourceValue, targetValue, _origin, parameterIndex, reason));
            }
        }

        protected override void HandleStoreField(MethodIL methodBody, int offset, FieldValue field, MultiValue valueToStore, int? parameterIndex)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, field, valueToStore, parameterIndex, field.Field.GetDisplayName());

        protected override void HandleStoreParameter(MethodIL methodBody, int offset, MethodParameterValue parameter, MultiValue valueToStore, int? parameterIndex)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, parameter, valueToStore, parameterIndex, parameter.Parameter.Method.GetDisplayName());

        protected override void HandleReturnValue(MethodIL methodBody, int offset, MethodReturnValue returnValue, MultiValue valueToStore)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, returnValue, valueToStore, null, returnValue.Method.GetDisplayName());

        protected override void HandleTypeTokenAccess(MethodIL methodBody, int offset, TypeDesc accessedType)
        {
            // Note that ldtoken alone is technically a reflection access to the type
            // it doesn't lead to full reflection marking of the type
            // since we implement full dataflow for type values and accesses to them.
            _origin = _origin.WithInstructionOffset(methodBody, offset);

            // Only check for generic instantiations.
            ProcessGenericArgumentDataFlow(accessedType);
        }

        protected override void HandleMethodTokenAccess(MethodIL methodBody, int offset, MethodDesc accessedMethod)
        {
            _origin = _origin.WithInstructionOffset(methodBody, offset);

            TrimAnalysisPatterns.Add(new TrimAnalysisTokenAccessPattern(accessedMethod, _origin));

            ProcessGenericArgumentDataFlow(accessedMethod);
        }

        protected override void HandleFieldTokenAccess(MethodIL methodBody, int offset, FieldDesc accessedField)
        {
            _origin = _origin.WithInstructionOffset(methodBody, offset);

            TrimAnalysisPatterns.Add(new TrimAnalysisTokenAccessPattern(accessedField, _origin));

            ProcessGenericArgumentDataFlow(accessedField);
        }

        public override MultiValue HandleCall(MethodIL callingMethodBody, MethodDesc calledMethod, ILOpcode operation, int offset, ValueNodeList methodParams)
        {
            Debug.Assert(callingMethodBody.OwningMethod == _origin.MemberDefinition);

            _origin = _origin.WithInstructionOffset(callingMethodBody, offset);

            MultiValue instanceValue;
            ImmutableArray<MultiValue> arguments;
            if (!calledMethod.Signature.IsStatic)
            {
                instanceValue = methodParams[0];
                arguments = methodParams.Skip(1).ToImmutableArray();
            }
            else
            {
                instanceValue = MultiValueLattice.Top;
                arguments = methodParams.ToImmutableArray();
            }

            TrimAnalysisPatterns.Add(new TrimAnalysisMethodCallPattern(
                callingMethodBody,
                operation,
                offset,
                calledMethod,
                instanceValue,
                arguments,
                _origin
            ));

            ProcessGenericArgumentDataFlow(calledMethod);

            var diagnosticContext = new DiagnosticContext(_origin, diagnosticsEnabled: false, _logger);
            return HandleCall(
                callingMethodBody,
                calledMethod,
                operation,
                instanceValue,
                arguments,
                diagnosticContext,
                _reflectionMarker);
        }

        public static MultiValue HandleCall(
            MethodIL callingMethodBody,
            MethodDesc calledMethod,
            ILOpcode operation,
            MultiValue instanceValue,
            ImmutableArray<MultiValue> argumentValues,
            DiagnosticContext diagnosticContext,
            ReflectionMarker reflectionMarker)
        {
            var callingMethodDefinition = callingMethodBody.OwningMethod;
            Debug.Assert(callingMethodDefinition == diagnosticContext.Origin.MemberDefinition);

            bool isNewObj = operation == ILOpcode.newobj;
            var annotatedMethodReturnValue = reflectionMarker.Annotations.GetMethodReturnValue(calledMethod, isNewObj);
            Debug.Assert(
                RequiresReflectionMethodBodyScannerForCallSite(reflectionMarker.Annotations, calledMethod) ||
                annotatedMethodReturnValue.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.None);

            var handleCallAction = new HandleCallAction(reflectionMarker.Annotations, operation, reflectionMarker, diagnosticContext, callingMethodDefinition, calledMethod.GetDisplayName());
            var intrinsicId = Intrinsics.GetIntrinsicIdForMethod(calledMethod);
            if (!handleCallAction.Invoke(calledMethod, instanceValue, argumentValues, intrinsicId, out MultiValue methodReturnValue))
                throw new NotImplementedException($"Unhandled intrinsic {intrinsicId}");
            return methodReturnValue;
        }

        private static bool IsAotUnsafeDelegate(TypeDesc parameterType)
        {
            TypeSystemContext context = parameterType.Context;
            return parameterType.IsWellKnownType(Internal.TypeSystem.WellKnownType.MulticastDelegate)
                    || parameterType == context.GetWellKnownType(Internal.TypeSystem.WellKnownType.MulticastDelegate).BaseType;
        }

        private static bool IsComInterop(MarshalAsDescriptor? marshalInfoProvider, TypeDesc parameterType)
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

        private void ProcessGenericArgumentDataFlow(MethodDesc method)
        {
            // We only need to validate static methods and then all generic methods
            // Instance non-generic methods don't need validation because the creation of the instance
            // is the place where the validation will happen.
            if (!method.Signature.IsStatic && !method.HasInstantiation && !method.IsConstructor)
                return;

            if (GenericArgumentDataFlow.RequiresGenericArgumentDataFlow(_annotations, method))
            {
                TrimAnalysisPatterns.Add(new TrimAnalysisGenericInstantiationAccessPattern(method, _origin));
            }
        }

        private void ProcessGenericArgumentDataFlow(FieldDesc field)
        {
            // We only need to validate static field accesses, instance field accesses don't need generic parameter validation
            // because the create of the instance would do that instead.
            if (!field.IsStatic)
                return;

            if (GenericArgumentDataFlow.RequiresGenericArgumentDataFlow(_annotations, field))
            {
                TrimAnalysisPatterns.Add(new TrimAnalysisGenericInstantiationAccessPattern(field, _origin));
            }
        }

        private void ProcessGenericArgumentDataFlow(TypeDesc type)
        {
            if (type.HasInstantiation && _annotations.HasGenericParameterAnnotation(type))
            {
                TrimAnalysisPatterns.Add(new TrimAnalysisGenericInstantiationAccessPattern(type, _origin));
            }
        }

        internal static bool IsPInvokeDangerous(MethodDesc calledMethod, out bool comDangerousMethod, out bool aotUnsafeDelegate)
        {
            if (!calledMethod.IsPInvoke)
            {
                comDangerousMethod = false;
                aotUnsafeDelegate = false;
                return false;
            }

            ParameterMetadata[] paramMetadata = calledMethod.GetParameterMetadata();

            ParameterMetadata returnParamMetadata = Array.Find(paramMetadata, m => m.Index == 0);

            aotUnsafeDelegate = IsAotUnsafeDelegate(calledMethod.Signature.ReturnType);
            comDangerousMethod = IsComInterop(returnParamMetadata.MarshalAsDescriptor, calledMethod.Signature.ReturnType);
            for (int paramIndex = 0; paramIndex < calledMethod.Signature.Length; paramIndex++)
            {
                MarshalAsDescriptor? marshalAsDescriptor = null;
                for (int metadataIndex = 0; metadataIndex < paramMetadata.Length; metadataIndex++)
                {
                    if (paramMetadata[metadataIndex].Index == paramIndex + 1)
                        marshalAsDescriptor = paramMetadata[metadataIndex].MarshalAsDescriptor;
                }

                aotUnsafeDelegate |= IsAotUnsafeDelegate(calledMethod.Signature[paramIndex]);
                comDangerousMethod |= IsComInterop(marshalAsDescriptor, calledMethod.Signature[paramIndex]);
            }

            return aotUnsafeDelegate || comDangerousMethod;
        }
    }
}
