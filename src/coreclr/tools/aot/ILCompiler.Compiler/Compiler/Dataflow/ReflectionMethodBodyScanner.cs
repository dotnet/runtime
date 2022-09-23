// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILCompiler.Logging;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Internal.IL;
using Internal.TypeSystem;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using InteropTypes = Internal.TypeSystem.Interop.InteropTypes;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using NodeFactory = ILCompiler.DependencyAnalysis.NodeFactory;
using WellKnownType = ILLink.Shared.TypeSystemProxy.WellKnownType;

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

        public static bool RequiresReflectionMethodBodyScannerForCallSite(FlowAnnotations flowAnnotations, MethodDesc methodDefinition)
        {
            return Intrinsics.GetIntrinsicIdForMethod(methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
                flowAnnotations.RequiresDataflowAnalysis(methodDefinition) ||
                methodDefinition.DoesMethodRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _) ||
                methodDefinition.DoesMethodRequire(DiagnosticUtilities.RequiresDynamicCodeAttribute, out _) ||
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
                fieldDefinition.DoesFieldRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _) ||
                fieldDefinition.DoesFieldRequire(DiagnosticUtilities.RequiresDynamicCodeAttribute, out _);
        }

        internal static void CheckAndReportRequires(in DiagnosticContext diagnosticContext, TypeSystemEntity calledMember, string requiresAttributeName)
        {
            if (!calledMember.DoesMemberRequire(requiresAttributeName, out var requiresAttribute))
                return;

            DiagnosticId diagnosticId = requiresAttributeName switch
            {
                DiagnosticUtilities.RequiresUnreferencedCodeAttribute => DiagnosticId.RequiresUnreferencedCode,
                DiagnosticUtilities.RequiresDynamicCodeAttribute => DiagnosticId.RequiresDynamicCode,
                DiagnosticUtilities.RequiresAssemblyFilesAttribute => DiagnosticId.RequiresAssemblyFiles,
                _ => throw new NotImplementedException($"{requiresAttributeName} is not a valid supported Requires attribute"),
            };

            string arg1 = MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage(requiresAttribute.Value));
            string arg2 = MessageFormat.FormatRequiresAttributeUrlArg(DiagnosticUtilities.GetRequiresAttributeUrl(requiresAttribute.Value));

            diagnosticContext.AddDiagnostic(diagnosticId, calledMember.GetDisplayName(), arg1, arg2);
        }

        private ReflectionMethodBodyScanner(NodeFactory factory, FlowAnnotations annotations, Logger logger, MessageOrigin origin)
            : base(annotations)
        {
            _logger = logger;
            _factory = factory;
            _origin = origin;
            _reflectionMarker = new ReflectionMarker(logger, factory, annotations, typeHierarchyDataFlow: false, enabled: false);
            TrimAnalysisPatterns = new TrimAnalysisPatternStore(MultiValueLattice, logger);
        }

        public override void InterproceduralScan(MethodIL methodBody)
        {
            base.InterproceduralScan(methodBody);

            // Replace the reflection marker with one which actually marks
            _reflectionMarker = new ReflectionMarker(_logger, _factory, _annotations, typeHierarchyDataFlow: false, enabled: true);
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
                    HandleAssignmentPattern(_origin, ReturnValue, methodReturnValue, new MethodOrigin(method));
            }
        }

        public static DependencyList ScanAndProcessReturnValue(NodeFactory factory, FlowAnnotations annotations, Logger logger, MethodIL methodBody)
        {
            var scanner = new ReflectionMethodBodyScanner(factory, annotations, logger, new MessageOrigin(methodBody.OwningMethod));

            scanner.InterproceduralScan(methodBody);

            return scanner._reflectionMarker.Dependencies;
        }

        public static DependencyList ProcessTypeGetTypeDataflow(NodeFactory factory, FlowAnnotations flowAnnotations, Logger logger, MetadataType type)
        {
            DynamicallyAccessedMemberTypes annotation = flowAnnotations.GetTypeAnnotation(type);
            Debug.Assert(annotation != DynamicallyAccessedMemberTypes.None);
            var reflectionMarker = new ReflectionMarker(logger, factory, flowAnnotations, typeHierarchyDataFlow: true, enabled: true);
            reflectionMarker.MarkTypeForDynamicallyAccessedMembers(new MessageOrigin(type), type, annotation, new TypeOrigin(type));
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

        protected override ValueWithDynamicallyAccessedMembers GetMethodParameterValue(MethodDesc method, int parameterIndex)
            => GetMethodParameterValue(method, parameterIndex, _annotations.GetParameterAnnotation(method, parameterIndex));

        private ValueWithDynamicallyAccessedMembers GetMethodParameterValue(MethodDesc method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
        {
            if (!method.Signature.IsStatic)
            {
                if (parameterIndex == 0)
                    return _annotations.GetMethodThisParameterValue(method, dynamicallyAccessedMemberTypes);

                parameterIndex--;
            }

            return _annotations.GetMethodParameterValue(method, parameterIndex, dynamicallyAccessedMemberTypes);
        }

        protected override MultiValue GetFieldValue(FieldDesc field) => _annotations.GetFieldValue(field);

        private void HandleStoreValueWithDynamicallyAccessedMembers(MethodIL methodBody, int offset, ValueWithDynamicallyAccessedMembers targetValue, MultiValue sourceValue, Origin memberWithRequirements)
        {
            // We must record all field accesses since we need to check RUC/RDC/RAF attributes on them regardless of annotations
            if (targetValue.DynamicallyAccessedMemberTypes != 0 || targetValue is FieldValue)
            {
                _origin = _origin.WithInstructionOffset(methodBody, offset);
                HandleAssignmentPattern(_origin, sourceValue, targetValue, memberWithRequirements);
            }
        }

        protected override void HandleStoreField(MethodIL methodBody, int offset, FieldValue field, MultiValue valueToStore)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, field, valueToStore, new FieldOrigin(field.Field));

        protected override void HandleStoreParameter(MethodIL methodBody, int offset, MethodParameterValue parameter, MultiValue valueToStore)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, parameter, valueToStore, parameter.ParameterOrigin);

        protected override void HandleStoreMethodThisParameter(MethodIL methodBody, int offset, MethodThisParameterValue thisParameter, MultiValue valueToStore)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, thisParameter, valueToStore, new ParameterOrigin(thisParameter.Method, 0));

        protected override void HandleStoreMethodReturnValue(MethodIL methodBody, int offset, MethodReturnValue returnValue, MultiValue valueToStore)
            => HandleStoreValueWithDynamicallyAccessedMembers(methodBody, offset, returnValue, valueToStore, new MethodOrigin(returnValue.Method));

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

            var diagnosticContext = new DiagnosticContext(_origin, diagnosticsEnabled: false, _logger);
            return HandleCall(
                callingMethodBody,
                calledMethod,
                operation,
                offset,
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
            int offset,
            MultiValue instanceValue,
            ImmutableArray<MultiValue> argumentValues,
            DiagnosticContext diagnosticContext,
            ReflectionMarker reflectionMarker,
            out MultiValue methodReturnValue)
        {
            var callingMethodDefinition = callingMethodBody.OwningMethod;
            Debug.Assert(callingMethodDefinition == diagnosticContext.Origin.MemberDefinition);

            bool requiresDataFlowAnalysis = reflectionMarker.Annotations.RequiresDataflowAnalysis(calledMethod);
            var annotatedMethodReturnValue = reflectionMarker.Annotations.GetMethodReturnValue(calledMethod);
            Debug.Assert(requiresDataFlowAnalysis || annotatedMethodReturnValue.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.None);

            MultiValue? maybeMethodReturnValue = null;

            var handleCallAction = new HandleCallAction(reflectionMarker.Annotations, reflectionMarker, diagnosticContext, callingMethodDefinition, new MethodOrigin(calledMethod));

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
                case var fieldOrPropertyIntrinsic when fieldOrPropertyIntrinsic == IntrinsicId.Expression_Field || fieldOrPropertyIntrinsic == IntrinsicId.Expression_Property:
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
                        bool result = handleCallAction.Invoke(calledMethod, instanceValue, argumentValues, out methodReturnValue, out _);

                        // Special case some intrinsics for AOT handling (on top of the trimming handling done in the HandleCallAction)
                        switch (intrinsicId)
                        {
                            case IntrinsicId.Type_MakeGenericType:
                            case IntrinsicId.MethodInfo_MakeGenericMethod:
                                CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresDynamicCodeAttribute);
                                break;
                        }

                        return result;
                    }

                case IntrinsicId.None:
                    {
                        if (calledMethod.IsPInvoke)
                        {
                            // Is the PInvoke dangerous?
                            ParameterMetadata[] paramMetadata = calledMethod.GetParameterMetadata();

                            ParameterMetadata returnParamMetadata = Array.Find(paramMetadata, m => m.Index == 0);

                            bool aotUnsafeDelegate = IsAotUnsafeDelegate(calledMethod.Signature.ReturnType);
                            bool comDangerousMethod = IsComInterop(returnParamMetadata.MarshalAsDescriptor, calledMethod.Signature.ReturnType);
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

                            if (aotUnsafeDelegate)
                            {
                                diagnosticContext.AddDiagnostic(DiagnosticId.CorrectnessOfAbstractDelegatesCannotBeGuaranteed, calledMethod.GetDisplayName());
                            }

                            if (comDangerousMethod)
                            {
                                diagnosticContext.AddDiagnostic(DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethod.GetDisplayName());
                            }
                        }

                        CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresUnreferencedCodeAttribute);
                        CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresDynamicCodeAttribute);
                        CheckAndReportRequires(diagnosticContext, calledMethod, DiagnosticUtilities.RequiresAssemblyFilesAttribute);

                        return handleCallAction.Invoke(calledMethod, instanceValue, argumentValues, out methodReturnValue, out _);
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
                        foreach (var value in argumentValues[0])
                        {
                            if (value is SystemTypeValue systemTypeValue
                                && !systemTypeValue.RepresentedType.Type.IsGenericDefinition
                                && !systemTypeValue.RepresentedType.Type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                            {
                                if (systemTypeValue.RepresentedType.Type.IsEnum)
                                {
                                    reflectionMarker.Dependencies.Add(reflectionMarker.Factory.ConstructedTypeSymbol(systemTypeValue.RepresentedType.Type.MakeArrayType()), "Enum.GetValues");
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
                        foreach (var value in argumentValues[paramIndex])
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
                                        reflectionMarker.Dependencies.Add(reflectionMarker.Factory.ReflectableMethod(ctorMethod), "Marshal API");
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
                        foreach (var value in argumentValues[1])
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
                // System.Object
                //
                // GetType()
                //
                case IntrinsicId.Object_GetType:
                    {
                        foreach (var valueNode in instanceValue)
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

                            TypeDesc? staticType = (valueNode as IValueWithStaticType)?.StaticType;
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
                foreach (var uniqueValue in methodReturnValue)
                {
                    if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes)
                    {
                        if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag(annotatedMethodReturnValue.DynamicallyAccessedMemberTypes))
                            throw new InvalidOperationException($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName()} to {calledMethod.GetDisplayName()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
                    }
                    else if (uniqueValue is SystemTypeValue)
                    {
                        // SystemTypeValue can fulfill any requirement, so it's always valid
                        // The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
                    }
                    else
                    {
                        throw new InvalidOperationException($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName()} to {calledMethod.GetDisplayName()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
                    }
                }
            }

            return true;

            void AddReturnValue(MultiValue value)
            {
                maybeMethodReturnValue = (maybeMethodReturnValue is null) ? value : MultiValueLattice.Meet((MultiValue)maybeMethodReturnValue, value);
            }
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
            Origin memberWithRequirements)
        {
            TrimAnalysisPatterns.Add(new TrimAnalysisAssignmentPattern(value, targetValue, origin, memberWithRequirements));
        }
    }
}
