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

            if (!methodBody.OwningMethod.Signature.ReturnType.IsVoid)
            {
                var method = methodBody.OwningMethod;
                var methodReturnValue = _annotations.GetMethodReturnValue(method);
                if (methodReturnValue.DynamicallyAccessedMemberTypes != 0)
                    HandleAssignmentPattern(_origin, ReturnValue, methodReturnValue, method.GetDisplayName());
            }
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

        private void HandleStoreValueWithDynamicallyAccessedMembers(MethodIL methodBody, int offset, ValueWithDynamicallyAccessedMembers targetValue, MultiValue sourceValue, string reason)
        {
            if (targetValue.DynamicallyAccessedMemberTypes != 0)
            {
                _origin = _origin.WithInstructionOffset(methodBody, offset);
                HandleAssignmentPattern(_origin, sourceValue, targetValue, reason);
            }
        }

        protected override void HandleStoreField(MethodIL methodBody, int offset, FieldValue field, MultiValue valueToStore)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, field, valueToStore, field.Field.GetDisplayName());

        protected override void HandleStoreParameter(MethodIL methodBody, int offset, MethodParameterValue parameter, MultiValue valueToStore)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, parameter, valueToStore, parameter.Parameter.Method.GetDisplayName());

        protected override void HandleStoreMethodReturnValue(MethodIL methodBody, int offset, MethodReturnValue returnValue, MultiValue valueToStore)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, returnValue, valueToStore, returnValue.Method.GetDisplayName());

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

        public override bool HandleCall(MethodIL callingMethodBody, MethodDesc calledMethod, ILOpcode operation, int offset, ValueNodeList methodParams, out MultiValue methodReturnValue)
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
                _reflectionMarker,
                out methodReturnValue);
        }

        public static bool HandleCall(
            MethodIL callingMethodBody,
            MethodDesc calledMethod,
            ILOpcode operation,
            MultiValue instanceValue,
            ImmutableArray<MultiValue> argumentValues,
            DiagnosticContext diagnosticContext,
            ReflectionMarker reflectionMarker,
            out MultiValue methodReturnValue)
        {
            var callingMethodDefinition = callingMethodBody.OwningMethod;
            Debug.Assert(callingMethodDefinition == diagnosticContext.Origin.MemberDefinition);

            var annotatedMethodReturnValue = reflectionMarker.Annotations.GetMethodReturnValue(calledMethod);
            Debug.Assert(
                RequiresReflectionMethodBodyScannerForCallSite(reflectionMarker.Annotations, calledMethod) ||
                annotatedMethodReturnValue.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.None);

            MultiValue? maybeMethodReturnValue = null;

            var handleCallAction = new HandleCallAction(reflectionMarker.Annotations, reflectionMarker, diagnosticContext, callingMethodDefinition, calledMethod.GetDisplayName());

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
                case IntrinsicId.Type_GetConstructors__BindingFlags:
                case IntrinsicId.Type_GetMethods__BindingFlags:
                case IntrinsicId.Type_GetFields__BindingFlags:
                case IntrinsicId.Type_GetProperties__BindingFlags:
                case IntrinsicId.Type_GetEvents__BindingFlags:
                case IntrinsicId.Type_GetNestedTypes__BindingFlags:
                case IntrinsicId.Type_GetMembers__BindingFlags:
                case IntrinsicId.Type_GetField:
                case IntrinsicId.Type_GetProperty:
                case IntrinsicId.Type_GetEvent:
                case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent:
                case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField:
                case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod:
                case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty:
                case IntrinsicId.Type_GetMember:
                case IntrinsicId.Type_GetMethod:
                case IntrinsicId.Type_GetNestedType:
                case IntrinsicId.Nullable_GetUnderlyingType:
                case IntrinsicId.Expression_Property:
                case IntrinsicId.Expression_Field:
                case IntrinsicId.Type_get_BaseType:
                case IntrinsicId.Type_GetConstructor:
                case IntrinsicId.MethodBase_GetMethodFromHandle:
                case IntrinsicId.MethodBase_get_MethodHandle:
                case IntrinsicId.Expression_Call:
                case IntrinsicId.Expression_New:
                case IntrinsicId.Type_GetType:
                case IntrinsicId.Activator_CreateInstance__Type:
                case IntrinsicId.Activator_CreateInstance__AssemblyName_TypeName:
                case IntrinsicId.Activator_CreateInstanceFrom:
                case IntrinsicId.AppDomain_CreateInstance:
                case IntrinsicId.AppDomain_CreateInstanceAndUnwrap:
                case IntrinsicId.AppDomain_CreateInstanceFrom:
                case IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap:
                case IntrinsicId.Assembly_CreateInstance:
                    {
                        return handleCallAction.Invoke(calledMethod, instanceValue, argumentValues, intrinsicId, out methodReturnValue);
                    }

            case IntrinsicId.Type_MakeGenericType:
                    {
                        bool triggersWarning = false;

                        if (instanceValue.IsEmpty() || argumentValues[0].IsEmpty())
                        {
                            triggersWarning = true;
                        }
                        else
                        {
                            foreach (var value in instanceValue.AsEnumerable())
                            {
                                if (value is SystemTypeValue typeValue)
                                {
                                    TypeDesc typeInstantiated = typeValue.RepresentedType.Type;
                                    if (!typeInstantiated.IsGenericDefinition)
                                    {
                                        // Nothing to do, will fail at runtime
                                    }
                                    else if (TryGetMakeGenericInstantiation(callingMethodDefinition, argumentValues[0], out Instantiation inst, out bool isExact))
                                    {
                                        if (inst.Length == typeInstantiated.Instantiation.Length)
                                        {
                                            typeInstantiated = ((MetadataType)typeInstantiated).MakeInstantiatedType(inst);

                                            if (isExact)
                                            {
                                                reflectionMarker.MarkType(diagnosticContext.Origin, typeInstantiated, "MakeGenericType");
                                            }
                                            else
                                            {
                                                reflectionMarker.RuntimeDeterminedDependencies.Add(new MakeGenericTypeSite(typeInstantiated));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        triggersWarning = true;
                                    }

                                }
                                else if (value == NullValue.Instance)
                                {
                                    // Nothing to do
                                }
                                else
                                {
                                    // We don't know what type the `MakeGenericMethod` was called on
                                    triggersWarning = true;
                                }
                            }
                        }

                        if (triggersWarning)
                        {
                            CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresDynamicCodeAttribute);
                        }

                        // This intrinsic is relevant to both trimming and AOT - call into trimming logic as well.
                        return handleCallAction.Invoke(calledMethod, instanceValue, argumentValues, intrinsicId, out methodReturnValue);
                    }

                case IntrinsicId.MethodInfo_MakeGenericMethod:
                    {
                        bool triggersWarning = false;

                        if (instanceValue.IsEmpty())
                        {
                            triggersWarning = true;
                        }
                        else
                        {
                            foreach (var methodValue in instanceValue.AsEnumerable())
                            {
                                if (methodValue is SystemReflectionMethodBaseValue methodBaseValue)
                                {
                                    MethodDesc methodInstantiated = methodBaseValue.RepresentedMethod.Method;
                                    if (!methodInstantiated.IsGenericMethodDefinition)
                                    {
                                        // Nothing to do, will fail at runtime
                                    }
                                    else if (!methodInstantiated.OwningType.IsGenericDefinition
                                        && TryGetMakeGenericInstantiation(callingMethodDefinition, argumentValues[0], out Instantiation inst, out bool isExact))
                                    {
                                        if (inst.Length == methodInstantiated.Instantiation.Length)
                                        {
                                            methodInstantiated = methodInstantiated.MakeInstantiatedMethod(inst);

                                            if (isExact)
                                            {
                                                reflectionMarker.MarkMethod(diagnosticContext.Origin, methodInstantiated, "MakeGenericMethod");
                                            }
                                            else
                                            {
                                                reflectionMarker.RuntimeDeterminedDependencies.Add(new MakeGenericMethodSite(methodInstantiated));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // If the owning type is a generic definition, we can't help much.
                                        triggersWarning = true;
                                    }
                                }
                                else if (methodValue == NullValue.Instance)
                                {
                                    // Nothing to do
                                }
                                else
                                {
                                    // We don't know what method the `MakeGenericMethod` was called on
                                    triggersWarning = true;
                                }
                            }
                        }

                        if (triggersWarning)
                        {
                            CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresDynamicCodeAttribute);
                        }

                        // This intrinsic is relevant to both trimming and AOT - call into trimming logic as well.
                        return handleCallAction.Invoke(calledMethod, instanceValue, argumentValues, intrinsicId, out methodReturnValue);
                    }

                case IntrinsicId.None:
                    {
                        if (IsPInvokeDangerous(calledMethod, out bool comDangerousMethod, out bool aotUnsafeDelegate))
                        {
                            if (aotUnsafeDelegate)
                            {
                                diagnosticContext.AddDiagnostic(DiagnosticId.CorrectnessOfAbstractDelegatesCannotBeGuaranteed, calledMethod.GetDisplayName());
                            }

                            if (comDangerousMethod)
                            {
                                diagnosticContext.AddDiagnostic(DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethod.GetDisplayName());
                            }
                        }

                        CheckAndReportAllRequires(diagnosticContext, calledMethod);

                        return handleCallAction.Invoke(calledMethod, instanceValue, argumentValues, intrinsicId, out methodReturnValue);
                    }

                case IntrinsicId.TypeDelegator_Ctor:
                    {
                        // This is an identity function for analysis purposes
                        if (operation == ILOpcode.newobj)
                            AddReturnValue(argumentValues[0]);
                    }
                    break;

                case IntrinsicId.Array_Empty:
                    {
                        AddReturnValue(ArrayValue.Create(0, calledMethod.Instantiation[0]));
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
                        foreach (var value in argumentValues[0].AsEnumerable ())
                        {
                            if (value is SystemTypeValue systemTypeValue
                                && !systemTypeValue.RepresentedType.Type.IsGenericDefinition
                                && !systemTypeValue.RepresentedType.Type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                            {
                                if (systemTypeValue.RepresentedType.Type.IsEnum)
                                {
                                    reflectionMarker.Dependencies.Add(reflectionMarker.Factory.ReflectedType(systemTypeValue.RepresentedType.Type.MakeArrayType()), "Enum.GetValues");
                                }
                            }
                            else
                                CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresDynamicCodeAttribute);
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
                        foreach (var value in argumentValues[paramIndex].AsEnumerable ())
                        {
                            if (value is SystemTypeValue systemTypeValue
                                && !systemTypeValue.RepresentedType.Type.IsGenericDefinition
                                && !systemTypeValue.RepresentedType.Type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                            {
                                if (systemTypeValue.RepresentedType.Type.IsDefType)
                                {
                                    reflectionMarker.Dependencies.Add(reflectionMarker.Factory.StructMarshallingData((DefType)systemTypeValue.RepresentedType.Type), "Marshal API");
                                    if (intrinsicId == IntrinsicId.Marshal_PtrToStructure
                                        && systemTypeValue.RepresentedType.Type.GetParameterlessConstructor() is MethodDesc ctorMethod
                                        && !reflectionMarker.Factory.MetadataManager.IsReflectionBlocked(ctorMethod))
                                    {
                                        reflectionMarker.Dependencies.Add(reflectionMarker.Factory.ReflectedMethod(ctorMethod.GetCanonMethodTarget(CanonicalFormKind.Specific)), "Marshal API");
                                    }
                                }
                            }
                            else
                                CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresDynamicCodeAttribute);
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
                        foreach (var value in argumentValues[1].AsEnumerable ())
                        {
                            if (value is SystemTypeValue systemTypeValue
                                && !systemTypeValue.RepresentedType.Type.IsGenericDefinition
                                && !systemTypeValue.RepresentedType.Type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                            {
                                if (systemTypeValue.RepresentedType.Type.IsDelegate)
                                {
                                    reflectionMarker.Dependencies.Add(reflectionMarker.Factory.DelegateMarshallingData((DefType)systemTypeValue.RepresentedType.Type), "Marshal API");
                                }
                            }
                            else
                                CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresDynamicCodeAttribute);
                        }
                    }
                    break;

                //
                // System.Delegate
                //
                // get_Method ()
                //
                // System.Reflection.RuntimeReflectionExtensions
                //
                // GetMethodInfo (System.Delegate)
                //
                case IntrinsicId.RuntimeReflectionExtensions_GetMethodInfo:
                case IntrinsicId.Delegate_get_Method:
                    {
                        // Find the parameter: first is an instance method, second is an extension method.
                        MultiValue param = intrinsicId == IntrinsicId.RuntimeReflectionExtensions_GetMethodInfo
                            ? argumentValues[0] : instanceValue;

                        // If this is Delegate.Method accessed from RuntimeReflectionExtensions.GetMethodInfo, ignore
                        // because we handle the callsites to that one here as well.
                        if (Intrinsics.GetIntrinsicIdForMethod(callingMethodDefinition) == IntrinsicId.RuntimeReflectionExtensions_GetMethodInfo)
                            break;

                        foreach (var valueNode in param.AsEnumerable())
                        {
                            TypeDesc? staticType = (valueNode as IValueWithStaticType)?.StaticType?.Type;
                            if (staticType is null || !staticType.IsDelegate)
                            {
                                // The static type is unknown or something useless like Delegate or MulticastDelegate.
                                reflectionMarker.Dependencies.Add(reflectionMarker.Factory.ReflectedDelegate(null), "Delegate.Method access on unknown delegate type");
                            }
                            else
                            {
                                if (staticType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                                    reflectionMarker.Dependencies.Add(reflectionMarker.Factory.ReflectedDelegate(staticType.GetTypeDefinition()), "Delegate.Method access (on inexact type)");
                                else
                                    reflectionMarker.Dependencies.Add(reflectionMarker.Factory.ReflectedDelegate(staticType.ConvertToCanonForm(CanonicalFormKind.Specific)), "Delegate.Method access");
                            }
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
                        foreach (var valueNode in instanceValue.AsEnumerable ())
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

                            TypeDesc? staticType = (valueNode as IValueWithStaticType)?.StaticType?.Type;
                            if (staticType is null || (!staticType.IsDefType && !staticType.IsArray))
                            {
                                // We don't know anything about the type GetType was called on. Track this as a usual "result of a method call without any annotations"
                                AddReturnValue(reflectionMarker.Annotations.GetMethodReturnValue(calledMethod));
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
                                AddReturnValue(new SystemTypeValue(staticType));
                            }
                            else
                            {
                                Debug.Assert(staticType is MetadataType || staticType.IsArray);
                                MetadataType closestMetadataType = staticType is MetadataType mdType ?
                                    mdType : (MetadataType)reflectionMarker.Factory.TypeSystemContext.GetWellKnownType(Internal.TypeSystem.WellKnownType.Array);

                                var annotation = reflectionMarker.Annotations.GetTypeAnnotation(staticType);

                                if (annotation != default)
                                {
                                    reflectionMarker.Dependencies.Add(reflectionMarker.Factory.ObjectGetTypeFlowDependencies(closestMetadataType), "GetType called on this type");
                                }

                                // Return a value which is "unknown type" with annotation. For now we'll use the return value node
                                // for the method, which means we're loosing the information about which staticType this
                                // started with. For now we don't need it, but we can add it later on.
                                AddReturnValue(reflectionMarker.Annotations.GetMethodReturnValue(calledMethod, annotation));
                            }
                        }
                    }
                    break;

                //
                // string System.Reflection.Assembly.Location getter
                // string System.Reflection.AssemblyName.CodeBase getter
                // string System.Reflection.AssemblyName.EscapedCodeBase getter
                //
                case IntrinsicId.Assembly_get_Location:
                case IntrinsicId.AssemblyName_get_CodeBase:
                case IntrinsicId.AssemblyName_get_EscapedCodeBase:
                    diagnosticContext.AddDiagnostic(DiagnosticId.AvoidAssemblyLocationInSingleFile, calledMethod.GetDisplayName());
                    break;

                //
                // string System.Reflection.Assembly.GetFile(string)
                // string System.Reflection.Assembly.GetFiles()
                // string System.Reflection.Assembly.GetFiles(bool)
                //
                case IntrinsicId.Assembly_GetFile:
                case IntrinsicId.Assembly_GetFiles:
                    diagnosticContext.AddDiagnostic(DiagnosticId.AvoidAssemblyGetFilesInSingleFile, calledMethod.GetDisplayName());
                    break;

                default:
                    throw new NotImplementedException("Unhandled intrinsic");
            }

            // If we get here, we handled this as an intrinsic.  As a convenience, if the code above
            // didn't set the return value (and the method has a return value), we will set it to be an
            // unknown value with the return type of the method.
            bool returnsVoid = calledMethod.Signature.ReturnType.IsVoid;
            methodReturnValue = maybeMethodReturnValue ?? (returnsVoid ?
                MultiValueLattice.Top :
                annotatedMethodReturnValue);

            // Validate that the return value has the correct annotations as per the method return value annotations
            if (annotatedMethodReturnValue.DynamicallyAccessedMemberTypes != 0)
            {
                foreach (var uniqueValue in methodReturnValue.AsEnumerable ())
                {
                    if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes)
                    {
                        if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag(annotatedMethodReturnValue.DynamicallyAccessedMemberTypes))
                            throw new InvalidOperationException($"Internal trimming error: processing of call from {callingMethodDefinition.GetDisplayName()} to {calledMethod.GetDisplayName()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
                    }
                    else if (uniqueValue is SystemTypeValue)
                    {
                        // SystemTypeValue can fulfill any requirement, so it's always valid
                        // The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
                    }
                    else
                    {
                        throw new InvalidOperationException($"Internal trimming error: processing of call from {callingMethodDefinition.GetDisplayName()} to {calledMethod.GetDisplayName()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
                    }
                }
            }

            return true;

            void AddReturnValue(MultiValue value)
            {
                maybeMethodReturnValue = (maybeMethodReturnValue is null) ? value : MultiValueLattice.Meet((MultiValue)maybeMethodReturnValue, value);
            }
        }

        private static bool TryGetMakeGenericInstantiation(
            MethodDesc contextMethod,
            in MultiValue genericParametersArray,
            out Instantiation inst,
            out bool isExact)
        {
            // We support calling MakeGeneric APIs with a very concrete instantiation array.
            // Only the form of `new Type[] { typeof(Foo), typeof(T), typeof(Foo<T>) }` is supported.

            inst = default;
            isExact = true;
            Debug.Assert(contextMethod.GetTypicalMethodDefinition() == contextMethod);

            var typesValue = genericParametersArray.AsSingleValue();
            if (typesValue is NullValue)
            {
                // This will fail at runtime but no warning needed
                inst = Instantiation.Empty;
                return true;
            }

            // Is this an array we model?
            if (typesValue is not ArrayValue array)
            {
                return false;
            }

            int? size = array.Size.AsConstInt();
            if (size == null)
            {
                return false;
            }

            TypeDesc[]? sigInst = null;
            TypeDesc[]? defInst = null;

            ArrayBuilder<TypeDesc> result = default;
            for (int i = 0; i < size.Value; i++)
            {
                // Go over each element of the array. If the value is unknown, bail.
                if (!array.TryGetValueByIndex(i, out MultiValue value))
                {
                    return false;
                }

                var singleValue = value.AsSingleValue();

                TypeDesc? type = singleValue switch
                {
                    SystemTypeValue systemType => systemType.RepresentedType.Type,
                    GenericParameterValue genericParamType => genericParamType.GenericParameter.GenericParameter,
                    NullableSystemTypeValue nullableSystemType => nullableSystemType.NullableType.Type,
                    _ => null
                };

                if (type is null)
                {
                    return false;
                }

                // type is now some type.
                // Because dataflow analysis oddly operates on method bodies instantiated over
                // generic parameters (as opposed to instantiated over signature variables)
                // We need to swap generic parameters (T, U,...) for signature variables (!0, !!1,...).
                // We need to do this for both generic parameters of the owning type, and generic
                // parameters of the owning method.
                if (type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                {
                    if (sigInst == null)
                    {
                        TypeDesc contextType = contextMethod.OwningType;
                        sigInst = new TypeDesc[contextType.Instantiation.Length + contextMethod.Instantiation.Length];
                        defInst = new TypeDesc[contextType.Instantiation.Length + contextMethod.Instantiation.Length];
                        TypeSystemContext context = type.Context;
                        for (int j = 0; j < contextType.Instantiation.Length; j++)
                        {
                            sigInst[j] = context.GetSignatureVariable(j, method: false);
                            defInst[j] = contextType.Instantiation[j];
                        }
                        for (int j = 0; j < contextMethod.Instantiation.Length; j++)
                        {
                            sigInst[j + contextType.Instantiation.Length] = context.GetSignatureVariable(j, method: true);
                            defInst[j + contextType.Instantiation.Length] = contextMethod.Instantiation[j];
                        }
                    }

                    isExact = false;

                    // defInst is [T, U, V], sigInst is `[!0, !!0, !!1]`.
                    type = type.ReplaceTypesInConstructionOfType(defInst, sigInst);
                }

                result.Add(type);
            }

            inst = new Instantiation(result.ToArray());
            return true;
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

        private void HandleAssignmentPattern(
            in MessageOrigin origin,
            in MultiValue value,
            ValueWithDynamicallyAccessedMembers targetValue,
            string reason)
        {
            TrimAnalysisPatterns.Add(new TrimAnalysisAssignmentPattern(value, targetValue, origin, reason));
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

        private static bool IsPInvokeDangerous(MethodDesc calledMethod, out bool comDangerousMethod, out bool aotUnsafeDelegate)
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

        private class MakeGenericMethodSite : INodeWithRuntimeDeterminedDependencies
        {
            private readonly MethodDesc _method;

            public MakeGenericMethodSite(MethodDesc method) => _method = method;

            public IEnumerable<DependencyNodeCore<NodeFactory>.DependencyListEntry> InstantiateDependencies(NodeFactory factory, Instantiation typeInstantiation, Instantiation methodInstantiation)
            {
                var list = new DependencyList();
                RootingHelpers.TryGetDependenciesForReflectedMethod(ref list, factory, _method.InstantiateSignature(typeInstantiation, methodInstantiation), "MakeGenericMethod");
                return list;
            }
        }

        private class MakeGenericTypeSite : INodeWithRuntimeDeterminedDependencies
        {
            private readonly TypeDesc _type;

            public MakeGenericTypeSite(TypeDesc type) => _type = type;

            public IEnumerable<DependencyNodeCore<NodeFactory>.DependencyListEntry> InstantiateDependencies(NodeFactory factory, Instantiation typeInstantiation, Instantiation methodInstantiation)
            {
                var list = new DependencyList();
                RootingHelpers.TryGetDependenciesForReflectedType(ref list, factory, _type.InstantiateSignature(typeInstantiation, methodInstantiation), "MakeGenericType");
                return list;
            }
        }
    }
}
