// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Node factory to be used during IL scanning.
    /// </summary>
    public sealed class ILScanNodeFactory : NodeFactory
    {
        public ILScanNodeFactory(CompilerTypeSystemContext context, CompilationModuleGroup compilationModuleGroup, MetadataManager metadataManager, InteropStubManager interopStubManager, NameMangler nameMangler, PreinitializationManager preinitManager)
            : base(context, compilationModuleGroup, metadataManager, interopStubManager, nameMangler, new LazyGenericsDisabledPolicy(), new LazyVTableSliceProvider(), new LazyDictionaryLayoutProvider(), new InlinedThreadStatics(), new ExternSymbolsImportedNodeProvider(), preinitManager, new DevirtualizationManager())
        {
        }

        protected override IMethodNode CreateMethodEntrypointNode(MethodDesc method)
        {
            if (method.IsInternalCall)
            {
                // TODO: come up with a scheme where this can be shared between codegen backends and the scanner
                if (TypeSystemContext.IsSpecialUnboxingThunkTargetMethod(method))
                {
                    return MethodEntrypoint(TypeSystemContext.GetRealSpecialUnboxingThunkTargetMethod(method));
                }
                else if (TypeSystemContext.IsDefaultInterfaceMethodImplementationThunkTargetMethod(method))
                {
                    return MethodEntrypoint(TypeSystemContext.GetRealDefaultInterfaceMethodImplementationThunkTargetMethod(method));
                }
                else if (method.IsArrayAddressMethod())
                {
                    return new ScannedMethodNode(((ArrayType)method.OwningType).GetArrayMethod(ArrayMethodKind.AddressWithHiddenArg));
                }
                else if (method.HasCustomAttribute("System.Runtime", "RuntimeImportAttribute"))
                {
                    return new RuntimeImportMethodNode(method, NameMangler);
                }
            }

            if (CompilationModuleGroup.ContainsMethodBody(method, false))
            {
                return new ScannedMethodNode(method);
            }
            else
            {
                return new ExternMethodSymbolNode(this, method);
            }
        }

        protected override IMethodNode CreateUnboxingStubNode(MethodDesc method)
        {
            Debug.Assert(!method.Signature.IsStatic);

            if (method.IsCanonicalMethod(CanonicalFormKind.Specific) && !method.HasInstantiation)
            {
                // Unboxing stubs to canonical instance methods need a special unboxing stub that unboxes
                // 'this' and also provides an instantiation argument (we do a calling convention conversion).
                // We don't do this for generic instance methods though because they don't use the MethodTable
                // for the generic context anyway.
                return new ScannedMethodNode(TypeSystemContext.GetSpecialUnboxingThunk(method, TypeSystemContext.GeneratedAssembly));
            }
            else
            {
                // Otherwise we just unbox 'this' and don't touch anything else.
                return new UnboxingStubNode(method);
            }
        }

        protected override ISymbolNode CreateReadyToRunHelperNode(ReadyToRunHelperKey helperCall)
        {
            return new ReadyToRunHelperNode(helperCall.HelperId, helperCall.Target);
        }
    }
}
