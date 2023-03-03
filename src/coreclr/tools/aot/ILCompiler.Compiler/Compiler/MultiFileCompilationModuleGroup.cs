// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public abstract class MultiFileCompilationModuleGroup : CompilationModuleGroup
    {
        private HashSet<ModuleDesc> _compilationModuleSet;

        public MultiFileCompilationModuleGroup(CompilerTypeSystemContext context, IEnumerable<ModuleDesc> compilationModuleSet)
        {
            _compilationModuleSet = new HashSet<ModuleDesc>(compilationModuleSet);

            // The fake assembly that holds compiler generated types is part of the compilation.
            _compilationModuleSet.Add(context.GeneratedAssembly);
        }

        public sealed override bool ContainsType(TypeDesc type)
        {
            EcmaType ecmaType = type as EcmaType;

            if (ecmaType == null)
                return true;

            if (!IsModuleInCompilationGroup(ecmaType.EcmaModule))
            {
                return false;
            }

            return true;
        }

        public sealed override bool ContainsTypeDictionary(TypeDesc type)
        {
            return ContainsType(type);
        }

        public sealed override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            if (method.HasInstantiation)
                return true;

            return ContainsType(method.OwningType);
        }

        public sealed override bool ContainsMethodDictionary(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) != method);
            return ContainsMethodBody(method, false);
        }

        public sealed override bool ImportsMethod(MethodDesc method, bool unboxingStub)
        {
            return false;
        }

        private bool IsModuleInCompilationGroup(EcmaModule module)
        {
            return _compilationModuleSet.Contains(module);
        }

        public sealed override bool IsSingleFileCompilation
        {
            get
            {
                return false;
            }
        }

        public sealed override bool ShouldReferenceThroughImportTable(TypeDesc type)
        {
            return false;
        }

        public override bool CanHaveReferenceThroughImportTable
        {
            get
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Represents a non-leaf multifile compilation group where types contained in the group are always fully expanded.
    /// </summary>
    public class MultiFileSharedCompilationModuleGroup : MultiFileCompilationModuleGroup
    {
        public MultiFileSharedCompilationModuleGroup(CompilerTypeSystemContext context, IEnumerable<ModuleDesc> compilationModuleSet)
            : base(context, compilationModuleSet)
        {
        }

        public override bool ShouldProduceFullVTable(TypeDesc type)
        {
            return ConstructedEETypeNode.CreationAllowed(type);
        }

        public override bool ShouldPromoteToFullType(TypeDesc type)
        {
            return ShouldProduceFullVTable(type);
        }

        public override bool PresenceOfEETypeImpliesAllMethodsOnType(TypeDesc type)
        {
            return (type.HasInstantiation || type.IsArray) && ShouldProduceFullVTable(type) &&
                   type.ConvertToCanonForm(CanonicalFormKind.Specific).IsCanonicalSubtype(CanonicalFormKind.Any);
        }

        public override bool AllowInstanceMethodOptimization(MethodDesc method)
        {
            // Both the instance methods and the owning type are homed in a single compilation group
            // so if we're able to generate the body, we would also generate the owning type here
            // and nowhere else.
            Debug.Assert(ContainsMethodBody(method, unboxingStub: false));
            TypeDesc owningType = method.OwningType;
            return owningType.IsDefType && !owningType.HasInstantiation && !method.HasInstantiation;
        }
    }
}
