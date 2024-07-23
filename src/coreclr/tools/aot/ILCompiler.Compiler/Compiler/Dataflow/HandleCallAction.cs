// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ILCompiler;
using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILLink.Shared.TypeSystemProxy;
using Internal.TypeSystem;
using Internal.IL;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using WellKnownType = ILLink.Shared.TypeSystemProxy.WellKnownType;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    internal partial struct HandleCallAction
    {
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

        private readonly ReflectionMarker _reflectionMarker;
        private ILOpcode _operation;
        private readonly MethodDesc _callingMethod;
        private readonly string _reason;

        public HandleCallAction(
            FlowAnnotations annotations,
            ILOpcode operation,
            ReflectionMarker reflectionMarker,
            in DiagnosticContext diagnosticContext,
            MethodDesc callingMethod,
            string reason)
        {
            _reflectionMarker = reflectionMarker;
            _operation = operation;
            _isNewObj = operation == ILOpcode.newobj;
            _diagnosticContext = diagnosticContext;
            _callingMethod = callingMethod;
            _annotations = annotations;
            _reason = reason;
            _requireDynamicallyAccessedMembersAction = new(reflectionMarker, diagnosticContext, reason);
        }

        private partial bool TryHandleIntrinsic (
            MethodProxy calledMethod,
            MultiValue instanceValue,
            IReadOnlyList<MultiValue> argumentValues,
            IntrinsicId intrinsicId,
            out MultiValue? methodReturnValue)
        {
            MultiValue? maybeMethodReturnValue = methodReturnValue = null;

            switch (intrinsicId)
            {
                case IntrinsicId.Type_MakeGenericType:
                    {
                        bool triggersWarning = false;

                        if (!instanceValue.IsEmpty() && !argumentValues[0].IsEmpty())
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
                                    else if (TryGetMakeGenericInstantiation(_callingMethod, argumentValues[0], out Instantiation inst, out bool isExact))
                                    {
                                        if (inst.Length == typeInstantiated.Instantiation.Length)
                                        {
                                            typeInstantiated = ((MetadataType)typeInstantiated).MakeInstantiatedType(inst);

                                            if (isExact)
                                            {
                                                _reflectionMarker.MarkType(_diagnosticContext.Origin, typeInstantiated, "MakeGenericType");
                                            }
                                            else
                                            {
                                                _reflectionMarker.RuntimeDeterminedDependencies.Add(new MakeGenericTypeSite(typeInstantiated));
                                            }
                                        }
                                    }
                                    else if (typeInstantiated.Instantiation.IsConstrainedToBeReferenceTypes())
                                    {
                                        // This will always succeed thanks to the runtime type loader
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
                                    // We don't know what type the `MakeGenericType` was called on
                                    triggersWarning = true;
                                }
                            }
                        }

                        if (triggersWarning)
                        {
                            ReflectionMethodBodyScanner.CheckAndReportRequires(_diagnosticContext, calledMethod.Method, DiagnosticUtilities.RequiresDynamicCodeAttribute);
                        }

                        // This intrinsic is relevant to both trimming and AOT - call into trimming logic as well.
                        return TryHandleSharedIntrinsic(calledMethod, instanceValue, argumentValues, intrinsicId, out methodReturnValue);
                    }

                case IntrinsicId.MethodInfo_MakeGenericMethod:
                    {
                        bool triggersWarning = false;

                        if (!instanceValue.IsEmpty())
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
                                        && TryGetMakeGenericInstantiation(_callingMethod, argumentValues[0], out Instantiation inst, out bool isExact))
                                    {
                                        if (inst.Length == methodInstantiated.Instantiation.Length)
                                        {
                                            methodInstantiated = methodInstantiated.MakeInstantiatedMethod(inst);

                                            if (isExact)
                                            {
                                                _reflectionMarker.MarkMethod(_diagnosticContext.Origin, methodInstantiated, "MakeGenericMethod");
                                            }
                                            else
                                            {
                                                _reflectionMarker.RuntimeDeterminedDependencies.Add(new MakeGenericMethodSite(methodInstantiated));
                                            }
                                        }
                                    }
                                    else if (methodInstantiated.Instantiation.IsConstrainedToBeReferenceTypes())
                                    {
                                        // This will always succeed thanks to the runtime type loader
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
                            ReflectionMethodBodyScanner.CheckAndReportRequires(_diagnosticContext, calledMethod.Method, DiagnosticUtilities.RequiresDynamicCodeAttribute);
                        }

                        // This intrinsic is relevant to both trimming and AOT - call into trimming logic as well.
                        return TryHandleSharedIntrinsic(calledMethod, instanceValue, argumentValues, intrinsicId, out methodReturnValue);
                    }

                case IntrinsicId.None:
                    {
                        if (ReflectionMethodBodyScanner.IsPInvokeDangerous(calledMethod.Method, out bool comDangerousMethod, out bool aotUnsafeDelegate))
                        {
                            if (aotUnsafeDelegate)
                            {
                                _diagnosticContext.AddDiagnostic(DiagnosticId.CorrectnessOfAbstractDelegatesCannotBeGuaranteed, calledMethod.GetDisplayName());
                            }

                            if (comDangerousMethod)
                            {
                                _diagnosticContext.AddDiagnostic(DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethod.GetDisplayName());
                            }
                        }

                        ReflectionMethodBodyScanner.CheckAndReportAllRequires(_diagnosticContext, calledMethod.Method);

                        return TryHandleSharedIntrinsic(calledMethod, instanceValue, argumentValues, intrinsicId, out methodReturnValue);
                    }

                case IntrinsicId.TypeDelegator_Ctor:
                    {
                        // This is an identity function for analysis purposes
                        if (_operation == ILOpcode.newobj)
                            AddReturnValue(argumentValues[0]);
                    }
                    break;

                case IntrinsicId.Array_Empty:
                    {
                        AddReturnValue(ArrayValue.Create(0, calledMethod.Method.Instantiation[0]));
                    }
                    break;

                //
                // System.Array
                //
                // CreateInstance (Type, Int32)
                //
                case IntrinsicId.Array_CreateInstance:
                    {
                        // We could try to analyze if the type is known, but for now making sure this works for canonical arrays is enough.
                        TypeDesc canonArrayType = _reflectionMarker.Factory.TypeSystemContext.CanonType.MakeArrayType();
                        _reflectionMarker.MarkType(_diagnosticContext.Origin, canonArrayType, "Array.CreateInstance was called");
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
                                    _reflectionMarker.Dependencies.Add(_reflectionMarker.Factory.ReflectedType(systemTypeValue.RepresentedType.Type.MakeArrayType()), "Enum.GetValues");
                                }
                            }
                            else
                                ReflectionMethodBodyScanner.CheckAndReportRequires(_diagnosticContext, calledMethod.Method, DiagnosticUtilities.RequiresDynamicCodeAttribute);
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
                                    _reflectionMarker.Dependencies.Add(_reflectionMarker.Factory.StructMarshallingData((DefType)systemTypeValue.RepresentedType.Type), "Marshal API");
                                    if (intrinsicId == IntrinsicId.Marshal_PtrToStructure
                                        && systemTypeValue.RepresentedType.Type.GetParameterlessConstructor() is MethodDesc ctorMethod
                                        && !_reflectionMarker.Factory.MetadataManager.IsReflectionBlocked(ctorMethod))
                                    {
                                        _reflectionMarker.Dependencies.Add(_reflectionMarker.Factory.ReflectedMethod(ctorMethod.GetCanonMethodTarget(CanonicalFormKind.Specific)), "Marshal API");
                                    }
                                }
                            }
                            else
                                ReflectionMethodBodyScanner.CheckAndReportRequires(_diagnosticContext, calledMethod.Method, DiagnosticUtilities.RequiresDynamicCodeAttribute);
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
                                    _reflectionMarker.Dependencies.Add(_reflectionMarker.Factory.DelegateMarshallingData((DefType)systemTypeValue.RepresentedType.Type), "Marshal API");
                                }
                            }
                            else
                                ReflectionMethodBodyScanner.CheckAndReportRequires(_diagnosticContext, calledMethod.Method, DiagnosticUtilities.RequiresDynamicCodeAttribute);
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
                        if (Intrinsics.GetIntrinsicIdForMethod(_callingMethod) == IntrinsicId.RuntimeReflectionExtensions_GetMethodInfo)
                            break;

                        foreach (var valueNode in param.AsEnumerable())
                        {
                            TypeDesc? staticType = (valueNode as IValueWithStaticType)?.StaticType?.Type;
                            if (staticType is null || !staticType.IsDelegate)
                            {
                                // The static type is unknown or something useless like Delegate or MulticastDelegate.
                                _reflectionMarker.Dependencies.Add(_reflectionMarker.Factory.ReflectedDelegate(null), "Delegate.Method access on unknown delegate type");
                            }
                            else
                            {
                                if (staticType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                                    _reflectionMarker.Dependencies.Add(_reflectionMarker.Factory.ReflectedDelegate(staticType.GetTypeDefinition()), "Delegate.Method access (on inexact type)");
                                else
                                    _reflectionMarker.Dependencies.Add(_reflectionMarker.Factory.ReflectedDelegate(staticType.ConvertToCanonForm(CanonicalFormKind.Specific)), "Delegate.Method access");
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
                            // It could be that T is annotated with for example PublicMethods:
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
                                DynamicallyAccessedMemberTypes annotation = default;
                                if (staticType is GenericParameterDesc genericParam)
                                {
                                    foreach (TypeDesc constraint in genericParam.TypeConstraints)
                                    {
                                        if (constraint.IsWellKnownType(Internal.TypeSystem.WellKnownType.Enum))
                                        {
                                            annotation = DynamicallyAccessedMemberTypes.PublicFields;
                                            break;
                                        }
                                    }
                                }

                                if (annotation != default)
                                {
                                    AddReturnValue(_reflectionMarker.Annotations.GetMethodReturnValue(calledMethod, _isNewObj, annotation));
                                }
                                else
                                {
                                    // We don't know anything about the type GetType was called on. Track this as a usual "result of a method call without any annotations"
                                    AddReturnValue(_reflectionMarker.Annotations.GetMethodReturnValue(calledMethod, _isNewObj));
                                }
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
                                    mdType : (MetadataType)_reflectionMarker.Factory.TypeSystemContext.GetWellKnownType(Internal.TypeSystem.WellKnownType.Array);

                                var annotation = _reflectionMarker.Annotations.GetTypeAnnotation(staticType);

                                if (annotation != default)
                                {
                                    _reflectionMarker.Dependencies.Add(_reflectionMarker.Factory.ObjectGetTypeFlowDependencies(closestMetadataType), "GetType called on this type");
                                }

                                // Return a value which is "unknown type" with annotation. For now we'll use the return value node
                                // for the method, which means we're loosing the information about which staticType this
                                // started with. For now we don't need it, but we can add it later on.
                                AddReturnValue(_reflectionMarker.Annotations.GetMethodReturnValue(calledMethod, _isNewObj, annotation));
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
                    _diagnosticContext.AddDiagnostic(DiagnosticId.AvoidAssemblyLocationInSingleFile, calledMethod.GetDisplayName());
                    break;

                //
                // string System.Reflection.Assembly.GetFile(string)
                // string System.Reflection.Assembly.GetFiles()
                // string System.Reflection.Assembly.GetFiles(bool)
                //
                case IntrinsicId.Assembly_GetFile:
                case IntrinsicId.Assembly_GetFiles:
                    _diagnosticContext.AddDiagnostic(DiagnosticId.AvoidAssemblyGetFilesInSingleFile, calledMethod.GetDisplayName());
                    break;

                default:
                    return false;
            }

            methodReturnValue = maybeMethodReturnValue;
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

        private partial bool MethodIsTypeConstructor(MethodProxy method)
        {
            if (!method.Method.IsConstructor)
                return false;
            TypeDesc? type = method.Method.OwningType;
            while (type is not null)
            {
                if (type.IsTypeOf(WellKnownType.System_Type))
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        private partial IEnumerable<SystemReflectionMethodBaseValue> GetMethodsOnTypeHierarchy(TypeProxy type, string name, BindingFlags? bindingFlags)
        {
            foreach (var method in type.Type.GetMethodsOnTypeHierarchy(m => m.Name == name, bindingFlags))
                yield return new SystemReflectionMethodBaseValue(new MethodProxy(method));
        }

        private partial IEnumerable<SystemTypeValue> GetNestedTypesOnType(TypeProxy type, string name, BindingFlags? bindingFlags)
        {
            foreach (var nestedType in type.Type.GetNestedTypesOnType(t => t.Name == name, bindingFlags))
                yield return new SystemTypeValue(new TypeProxy(nestedType));
        }

        private partial bool TryGetBaseType(TypeProxy type, out TypeProxy? baseType)
        {
            if (type.Type.BaseType != null)
            {
                baseType = new TypeProxy(type.Type.BaseType);
                return true;
            }

            baseType = null;
            return false;
        }

#pragma warning disable IDE0060
        private partial bool TryResolveTypeNameForCreateInstanceAndMark(in MethodProxy calledMethod, string assemblyName, string typeName, out TypeProxy resolvedType)
        {
            // TODO: niche APIs that we probably shouldn't even have added
            // We have to issue a warning, otherwise we could break the app without a warning.
            // This is not the ideal warning, but it's good enough for now.
            _diagnosticContext.AddDiagnostic(DiagnosticId.UnrecognizedParameterInMethodCreateInstance, calledMethod.GetParameter((ParameterIndex)(1 + (calledMethod.HasImplicitThis() ? 1 : 0))).GetDisplayName(), calledMethod.GetDisplayName());
            resolvedType = default;
            return false;
        }
#pragma warning restore IDE0060

        private partial void MarkStaticConstructor(TypeProxy type)
            => _reflectionMarker.MarkStaticConstructor(_diagnosticContext.Origin, type.Type, _reason);

        private partial void MarkEventsOnTypeHierarchy(TypeProxy type, string name, BindingFlags? bindingFlags)
            => _reflectionMarker.MarkEventsOnTypeHierarchy(_diagnosticContext.Origin, type.Type, e => e.Name == name, _reason, bindingFlags);

        private partial void MarkFieldsOnTypeHierarchy(TypeProxy type, string name, BindingFlags? bindingFlags)
            => _reflectionMarker.MarkFieldsOnTypeHierarchy(_diagnosticContext.Origin, type.Type, f => f.Name == name, _reason, bindingFlags);

        private partial void MarkPropertiesOnTypeHierarchy(TypeProxy type, string name, BindingFlags? bindingFlags)
            => _reflectionMarker.MarkPropertiesOnTypeHierarchy(_diagnosticContext.Origin, type.Type, p => p.Name == name, _reason, bindingFlags);

        private partial void MarkPublicParameterlessConstructorOnType(TypeProxy type)
            => _reflectionMarker.MarkConstructorsOnType(_diagnosticContext.Origin, type.Type, m => m.IsPublic() && !m.HasMetadataParameters(), _reason);

        private partial void MarkConstructorsOnType(TypeProxy type, BindingFlags? bindingFlags, int? parameterCount)
            => _reflectionMarker.MarkConstructorsOnType(_diagnosticContext.Origin, type.Type, parameterCount == null ? null : m => m.GetMetadataParametersCount() == parameterCount, _reason, bindingFlags);

        private partial void MarkMethod(MethodProxy method)
            => _reflectionMarker.MarkMethod(_diagnosticContext.Origin, method.Method, _reason);

        private partial void MarkType(TypeProxy type)
            => _reflectionMarker.MarkType(_diagnosticContext.Origin, type.Type, _reason);

        private partial bool MarkAssociatedProperty(MethodProxy method)
        {
            var propertyDefinition = method.Method.GetPropertyForAccessor();
            if (propertyDefinition is null)
            {
                return false;
            }

            _reflectionMarker.MarkProperty(_diagnosticContext.Origin, propertyDefinition, _reason);
            return true;
        }

        private partial string GetContainingSymbolDisplayName() => _callingMethod.GetDisplayName();

        private sealed class MakeGenericMethodSite : INodeWithRuntimeDeterminedDependencies
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

        private sealed class MakeGenericTypeSite : INodeWithRuntimeDeterminedDependencies
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

    file static class Extensions
    {
        public static bool IsConstrainedToBeReferenceTypes(this Instantiation inst)
        {
            foreach (GenericParameterDesc param in inst)
                if (!param.HasReferenceTypeConstraint)
                    return false;

            return true;
        }
    }
}
