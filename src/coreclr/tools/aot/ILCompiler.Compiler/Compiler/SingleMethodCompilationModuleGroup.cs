// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// A compilation group that only contains a single method. Useful for development purposes when investigating
    /// code generation issues.
    /// </summary>
    public class SingleMethodCompilationModuleGroup : CompilationModuleGroup
    {
        private MethodDesc _method;

        public SingleMethodCompilationModuleGroup(MethodDesc method)
        {
            _method = method;
        }

        public override bool IsSingleFileCompilation
        {
            get
            {
                return false;
            }
        }

        public override bool ContainsMethodBody(MethodDesc method, bool unboxingStub)
        {
            return method == _method;
        }

        public sealed override bool ContainsMethodDictionary(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) != method);
            return true;
        }

        public override bool ContainsType(TypeDesc type)
        {
            return true;
        }

        public override bool ContainsTypeDictionary(TypeDesc type)
        {
            return true;
        }

        public override bool ImportsMethod(MethodDesc method, bool unboxingStub)
        {
            return false;
        }

        public override bool ShouldProduceFullVTable(TypeDesc type)
        {
            return false;
        }

        public override bool ShouldPromoteToFullType(TypeDesc type)
        {
            return false;
        }

        public override bool PresenceOfEETypeImpliesAllMethodsOnType(TypeDesc type)
        {
            return false;
        }

        public override bool ShouldReferenceThroughImportTable(TypeDesc type)
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

        public override bool AllowInstanceMethodOptimization(MethodDesc method)
        {
            return false;
        }
    }
}
