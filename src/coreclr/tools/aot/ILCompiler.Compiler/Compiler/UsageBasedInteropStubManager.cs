// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Interop;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysis;
using ILLink.Shared;

using Debug = System.Diagnostics.Debug;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using System.Runtime.InteropServices;

namespace ILCompiler
{
    /// <summary>
    /// Represents an interop stub manager whose list of stubs is determined by statical usage seen in the compiled program.
    /// </summary>
    public class UsageBasedInteropStubManager : CompilerGeneratedInteropStubManager
    {
        private Logger _logger;

        public UsageBasedInteropStubManager(InteropStateManager interopStateManager, PInvokeILEmitterConfiguration pInvokeILEmitterConfiguration, Logger logger)
            : base(interopStateManager, pInvokeILEmitterConfiguration)
        {
            _logger = logger;
        }

        public override void AddDependenciesDueToPInvoke(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if (method.IsPInvoke && method.OwningType is MetadataType type && MarshalHelpers.IsRuntimeMarshallingEnabled(type.Module))
            {
                dependencies = dependencies ?? new DependencyList();

                MethodSignature methodSig = method.Signature;
                AddParameterMarshallingDependencies(ref dependencies, factory, method, methodSig.ReturnType);

                for (int i = 0; i < methodSig.Length; i++)
                {
                    AddParameterMarshallingDependencies(ref dependencies, factory, method, methodSig[i]);
                }
            }

            if (method.HasInstantiation)
            {
                dependencies = dependencies ?? new DependencyList();
                AddMarshalAPIsGenericDependencies(ref dependencies, factory, method);
            }
        }

        private void AddParameterMarshallingDependencies(ref DependencyList dependencies, NodeFactory factory, MethodDesc method, TypeDesc type)
        {
            if (type.IsDelegate)
            {
                dependencies.Add(factory.DelegateMarshallingData((DefType)type), "Delegate marshaling");
            }

            TypeSystemContext context = type.Context;
            if ((type.IsWellKnownType(WellKnownType.MulticastDelegate)
                    || type == context.GetWellKnownType(WellKnownType.MulticastDelegate).BaseType))
            {
                // If we hit this p/invoke as part of delegate marshalling (i.e. this is a delegate
                // that has another delegate in the signature), blame the delegate type, not the marshalling thunk.
                // This should ideally warn from the use site (e.g. where GetDelegateForFunctionPointer
                // is called) but it's currently hard to get a warning from those spots and this guarantees
                // we won't miss a spot (e.g. a p/invoke that has a delegate and that delegate contains
                // a System.Delegate parameter).
                MethodDesc reportedMethod = method;
                if (reportedMethod is Internal.IL.Stubs.DelegateMarshallingMethodThunk delegateThunkMethod)
                {
                    reportedMethod = delegateThunkMethod.InvokeMethod;
                }

                _logger.LogWarning(reportedMethod, DiagnosticId.CorrectnessOfAbstractDelegatesCannotBeGuaranteed, DiagnosticUtilities.GetMethodSignatureDisplayName(method));
            }

            // struct may contain delegate fields, hence we need to add dependencies for it
            if (type.IsByRef)
                type = ((ParameterizedType)type).ParameterType;

            if (MarshalHelpers.IsStructMarshallingRequired(type))
            {
                foreach (FieldDesc field in type.GetFields())
                {
                    if (field.IsStatic)
                        continue;

                    AddParameterMarshallingDependencies(ref dependencies, factory, method, field.FieldType);
                }
            }
        }

        public override void AddInterestingInteropConstructedTypeDependencies(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            if (type.IsDelegate)
            {
                var delegateType = (MetadataType)type;
                if (delegateType.HasCustomAttribute("System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute"))
                {
                    dependencies = dependencies ?? new DependencyList();
                    dependencies.Add(factory.DelegateMarshallingData(delegateType), "Delegate marshalling");
                }
            }
        }

        /// <summary>
        /// For Marshal generic APIs(eg. Marshal.StructureToPtr<T>, GetFunctionPointerForDelegate) we add
        /// the generic parameter as dependencies so that we can generate runtime data for them
        /// </summary>
        public override void AddMarshalAPIsGenericDependencies(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            Debug.Assert(method.HasInstantiation);

            TypeDesc owningType = method.OwningType;
            MetadataType metadataType = owningType as MetadataType;
            if (metadataType != null && metadataType.Module == factory.TypeSystemContext.SystemModule)
            {
                if (metadataType.Name == "Marshal" && metadataType.Namespace == "System.Runtime.InteropServices")
                {
                    string methodName = method.Name;
                    if (methodName == "GetFunctionPointerForDelegate" ||
                        methodName == "GetDelegateForFunctionPointer" ||
                        methodName == "PtrToStructure" ||
                        methodName == "StructureToPtr" ||
                        methodName == "SizeOf" ||
                        methodName == "OffsetOf")
                    {
                        foreach (TypeDesc type in method.Instantiation)
                        {
                            dependencies = dependencies ?? new DependencyList();
                            if (type.IsDelegate)
                            {
                                dependencies.Add(factory.DelegateMarshallingData((DefType)type), "Delegate marshlling");
                            }
                            else if (MarshalHelpers.IsStructMarshallingRequired(type) || (methodName == "OffsetOf" && type is DefType))
                            {
                                dependencies.Add(factory.StructMarshallingData((DefType)type), "Struct marshalling");
                            }
                        }
                    }
                }
            }
        }
    }
}
