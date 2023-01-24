// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Interop;
using ILCompiler.DependencyAnalysis;

using Debug = System.Diagnostics.Debug;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

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

        public override void AddDependenciesDueToMethodCodePresence(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if (method.HasInstantiation)
            {
                dependencies ??= new DependencyList();
                AddMarshalAPIsGenericDependencies(ref dependencies, factory, method);
            }
        }

        public override void AddInterestingInteropConstructedTypeDependencies(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            if (type.IsDelegate)
            {
                var delegateType = (MetadataType)type;
                if (delegateType.HasCustomAttribute("System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute"))
                {
                    dependencies ??= new DependencyList();
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
                            dependencies ??= new DependencyList();
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
